namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

public sealed class PaymentRepository(ApplicationDbContext context, IDateTimeProvider clock) : IPaymentRepository
{
    public Task<Order?> GetOrderForPaymentAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default) =>
        context.Orders.AsNoTracking().FirstOrDefaultAsync(order => order.Id == orderId && order.CustomerId == customerId && !order.IsDeleted, cancellationToken);

    public async Task<(Contract? Contract, ContractMilestone? Milestone)> GetMilestoneForPaymentAsync(Guid customerId, Guid contractId, Guid milestoneId, CancellationToken cancellationToken = default)
    {
        var contract = await context.Contracts.AsNoTracking().FirstOrDefaultAsync(item => item.Id == contractId
            && item.CustomerId == customerId && !item.IsDeleted, cancellationToken);
        if (contract is null) return (null, null);
        var milestone = await context.ContractMilestones.AsNoTracking().FirstOrDefaultAsync(item => item.Id == milestoneId
            && item.ContractId == contractId && !item.IsDeleted, cancellationToken);
        return (contract, milestone);
    }

    public Task<Payment?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default) =>
        context.Payments.AsNoTracking().FirstOrDefaultAsync(payment => payment.Id == paymentId && !payment.IsDeleted, cancellationToken);

    public Task<Payment?> GetPaymentByIdempotencyKeyAsync(Guid customerId, string idempotencyKey, CancellationToken cancellationToken = default) =>
        context.Payments.AsNoTracking().FirstOrDefaultAsync(payment => payment.CustomerId == customerId
            && payment.IdempotencyKey == idempotencyKey && !payment.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<Payment>> GetPaymentsAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default) => supplier
        ? await (from payment in context.Payments.AsNoTracking()
                 join order in context.Orders.AsNoTracking() on payment.OrderId equals order.Id into orders
                 from order in orders.DefaultIfEmpty()
                 join shop in context.Shops.AsNoTracking() on order!.ShopId equals shop.Id into shops
                 from shop in shops.DefaultIfEmpty()
                 join profile in context.SupplierProfiles.AsNoTracking() on shop!.SupplierId equals profile.Id into profiles
                 from profile in profiles.DefaultIfEmpty()
                 join contract in context.Contracts.AsNoTracking() on payment.ContractId equals contract.Id into contracts
                 from contract in contracts.DefaultIfEmpty()
                 join contractProfile in context.SupplierProfiles.AsNoTracking() on contract!.SupplierId equals contractProfile.Id into contractProfiles
                 from contractProfile in contractProfiles.DefaultIfEmpty()
                 where !payment.IsDeleted && ((profile != null && profile.UserId == userId) || (contractProfile != null && contractProfile.UserId == userId))
                 orderby payment.CreatedAt descending
                 select payment).Take(100).ToListAsync(cancellationToken)
        : await context.Payments.AsNoTracking().Where(payment => payment.CustomerId == userId && !payment.IsDeleted)
            .OrderByDescending(payment => payment.CreatedAt).Take(100).ToListAsync(cancellationToken);

    public async Task<bool> CreatePaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        if (payment.OrderId.HasValue && await context.Payments.AnyAsync(existing => existing.OrderId == payment.OrderId
                && (existing.Status == PaymentStatus.Pending || existing.Status == PaymentStatus.Processing || existing.Status == PaymentStatus.Completed)
                && !existing.IsDeleted, cancellationToken)) return false;
        if (payment.MilestoneId.HasValue && await context.Payments.AnyAsync(existing => existing.MilestoneId == payment.MilestoneId
                && (existing.Status == PaymentStatus.Pending || existing.Status == PaymentStatus.Processing) && !existing.IsDeleted, cancellationToken)) return false;
        context.Payments.Add(payment);
        context.Notifications.Add(new Notification
        {
            UserId = payment.CustomerId, Type = "payment.pending", Title = "Payment instructions ready",
            Content = $"Payment {payment.PaymentNumber} is pending. Use the payment number as the transfer reference.",
            ReferenceType = "payment", ReferenceId = payment.Id
        });
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsPaymentUniquenessConflict(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            return false;
        }
    }

    private static bool IsPaymentUniquenessConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "payments_idempotency_key_key" or "payments_one_open_payment_per_order_key" or "payments_one_open_payment_per_milestone_key"
        };

    public async Task<int> ExpirePendingPaymentsAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var now = clock.UtcNow;
        var expiredPayments = await context.Payments.FromSqlInterpolated($"SELECT * FROM payments WHERE status IN ('Pending'::payment_status, 'Processing'::payment_status) AND expires_at <= {now} AND is_deleted = FALSE FOR UPDATE SKIP LOCKED")
            .ToListAsync(cancellationToken);
        foreach (var payment in expiredPayments)
        {
            var previousStatus = payment.Status;
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedAt = now;
            context.WorkflowEvents.Add(new WorkflowEvent
            {
                AggregateType = "Payment", AggregateId = payment.Id, EventType = "PaymentStatusChanged",
                FromStatus = previousStatus.ToString(), ToStatus = PaymentStatus.Failed.ToString(), Reason = "Payment deadline elapsed."
            });
            await NotifyPaymentPartiesAsync(payment, "payment.expired", "Payment expired", $"Payment {payment.PaymentNumber} expired before it was confirmed.", cancellationToken);
        }
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return expiredPayments.Count;
    }

    public async Task<Payment?> ConfirmPaymentAsync(Guid paymentId, Guid adminUserId, string reason, string? ipAddress, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var payment = await context.Payments.FromSqlInterpolated($"SELECT * FROM payments WHERE id = {paymentId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        if (payment is null || payment.IsDeleted || payment.Status is not (PaymentStatus.Pending or PaymentStatus.Processing)) return null;
        var oldStatus = payment.Status;
        var now = clock.UtcNow;
        payment.Status = PaymentStatus.Completed;
        payment.PaidAt = now;
        payment.UpdatedAt = now;
        payment.UpdatedBy = adminUserId;
        if (payment.MilestoneId is Guid milestoneId && payment.ContractId is Guid contractId)
        {
            var milestone = await context.ContractMilestones.FirstOrDefaultAsync(item => item.Id == milestoneId && item.ContractId == contractId, cancellationToken);
            var contract = await context.Contracts.FirstOrDefaultAsync(item => item.Id == contractId && !item.IsDeleted, cancellationToken);
            if (milestone is null || contract is null) return null;
            milestone.Status = MilestoneStatus.Paid;
            milestone.UpdatedAt = now;
            milestone.UpdatedBy = adminUserId;
            if (milestone.IsDeposit && contract.Status == ContractStatus.PendingDeposit)
            {
                contract.Status = ContractStatus.Active;
                contract.DepositPaidAt = now;
                contract.UpdatedAt = now;
                contract.UpdatedBy = adminUserId;
                context.WorkflowEvents.Add(new WorkflowEvent
                {
                    AggregateType = "Contract", AggregateId = contract.Id, EventType = "ContractStatusChanged",
                    FromStatus = ContractStatus.PendingDeposit.ToString(), ToStatus = ContractStatus.Active.ToString(), ActorUserId = adminUserId,
                    Reason = "Mandatory deposit payment confirmed."
                });
                await NotifyContractPartiesAsync(contract, "contract.active", "Contract is active", "The required deposit was confirmed and the contract is now active.", cancellationToken);
            }
        }
        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "Payment", AggregateId = payment.Id, EventType = "PaymentStatusChanged",
            FromStatus = oldStatus.ToString(), ToStatus = PaymentStatus.Completed.ToString(), ActorUserId = adminUserId, Reason = reason
        });
        context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminUserId, Action = "PaymentManuallyConfirmed", EntityName = nameof(Payment), EntityId = payment.Id.ToString(),
            MetadataJson = JsonSerializer.Serialize(new { reason, payment.Amount, payment.PaymentNumber }), IpAddress = ipAddress, CreatedAt = now
        });
        await NotifyPaymentPartiesAsync(payment, "payment.completed", "Payment confirmed", $"Payment {payment.PaymentNumber} was confirmed.", cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return payment;
    }

    public async Task<Payment?> RefundPaymentAsync(Guid paymentId, Guid adminUserId, string reason, string? ipAddress, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var payment = await context.Payments.FromSqlInterpolated($"SELECT * FROM payments WHERE id = {paymentId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        if (payment is null || payment.IsDeleted || payment.Status != PaymentStatus.Completed) return null;
        var now = clock.UtcNow;
        payment.Status = PaymentStatus.Refunded;
        payment.RefundedAmount = payment.Amount;
        payment.RefundReason = reason;
        payment.UpdatedAt = now;
        payment.UpdatedBy = adminUserId;
        if (payment.MilestoneId is Guid milestoneId && payment.ContractId is Guid contractId)
        {
            var milestone = await context.ContractMilestones.FirstOrDefaultAsync(item => item.Id == milestoneId && item.ContractId == contractId, cancellationToken);
            var contract = await context.Contracts.FirstOrDefaultAsync(item => item.Id == contractId && !item.IsDeleted, cancellationToken);
            if (milestone?.IsDeposit == true && contract?.Status == ContractStatus.Active)
            {
                milestone.Status = MilestoneStatus.AwaitingPayment;
                contract.Status = ContractStatus.PendingDeposit;
                contract.DepositPaidAt = null;
                contract.UpdatedAt = now;
                context.WorkflowEvents.Add(new WorkflowEvent
                {
                    AggregateType = "Contract", AggregateId = contract.Id, EventType = "ContractStatusChanged",
                    FromStatus = ContractStatus.Active.ToString(), ToStatus = ContractStatus.PendingDeposit.ToString(), ActorUserId = adminUserId,
                    Reason = "The deposit was refunded."
                });
            }
        }
        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "Payment", AggregateId = payment.Id, EventType = "PaymentStatusChanged",
            FromStatus = PaymentStatus.Completed.ToString(), ToStatus = PaymentStatus.Refunded.ToString(), ActorUserId = adminUserId, Reason = reason
        });
        context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminUserId, Action = "PaymentRefunded", EntityName = nameof(Payment), EntityId = payment.Id.ToString(),
            MetadataJson = JsonSerializer.Serialize(new { reason, payment.RefundedAmount, payment.PaymentNumber }), IpAddress = ipAddress, CreatedAt = now
        });
        await NotifyPaymentPartiesAsync(payment, "payment.refunded", "Payment refunded", $"Payment {payment.PaymentNumber} was refunded.", cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return payment;
    }

    public async Task<Payment?> PrepareVnPayPaymentAsync(Guid customerId, Guid contractId, Guid milestoneId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var contract = await context.Contracts.FromSqlInterpolated(
                $"SELECT * FROM contracts WHERE id = {contractId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        var milestone = await context.ContractMilestones.FromSqlInterpolated(
                $"SELECT * FROM contract_milestones WHERE id = {milestoneId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (contract is null || milestone is null || contract.CustomerId != customerId ||
            milestone.ContractId != contractId ||
            (contract.Status != ContractStatus.PendingDeposit && contract.Status != ContractStatus.Active) ||
            milestone.Amount is not > 0)
            return null;

        var payment = await context.Payments.FromSqlInterpolated(
                $"SELECT * FROM payments WHERE milestone_id = {milestoneId} AND is_deleted = FALSE FOR UPDATE")
            .OrderByDescending(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        var now = clock.UtcNow;
        if (payment is null)
        {
            payment = new Payment
            {
                Id = Guid.NewGuid(), ContractId = contractId, MilestoneId = milestoneId, CustomerId = customerId,
                PaymentNumber = $"PAY-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant(),
                Amount = milestone.Amount.Value, Currency = "VND", Method = PaymentMethod.VNPay,
                Provider = "VNPay", Status = PaymentStatus.Pending, ExpiresAt = now.AddHours(48), CreatedBy = customerId
            };
            context.Payments.Add(payment);
        }
        else if (payment.Status == PaymentStatus.Completed)
        {
            return payment;
        }
        else if (payment.Status is not (PaymentStatus.Pending or PaymentStatus.Failed))
        {
            return null;
        }
        else
        {
            payment.Method = PaymentMethod.VNPay;
            payment.Provider = "VNPay";
            payment.Status = PaymentStatus.Pending;
            payment.ExpiresAt = now.AddHours(48);
            payment.UpdatedAt = now;
            payment.UpdatedBy = customerId;
        }
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return payment;
    }

    public async Task<Payment?> PrepareOrderVnPayPaymentAsync(Guid customerId, Guid orderId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var order = await context.Orders.FromSqlInterpolated(
                $"SELECT * FROM orders WHERE id = {orderId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null || order.CustomerId != customerId || order.IsDeleted ||
            order.Status == OrderStatus.Cancelled || order.TotalAmount <= 0)
            return null;

        var payment = await context.Payments.FromSqlInterpolated(
                $"SELECT * FROM payments WHERE order_id = {orderId} AND is_deleted = FALSE FOR UPDATE")
            .OrderByDescending(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        var now = clock.UtcNow;
        if (payment is null)
        {
            payment = new Payment
            {
                Id = Guid.NewGuid(), OrderId = orderId, CustomerId = customerId,
                PaymentNumber = $"PAY-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant(),
                Amount = order.TotalAmount, Currency = "VND", Method = PaymentMethod.VNPay,
                Provider = "VNPay", Status = PaymentStatus.Pending, ExpiresAt = now.AddHours(48), CreatedBy = customerId
            };
            context.Payments.Add(payment);
        }
        else if (payment.Status == PaymentStatus.Completed)
        {
            return payment;
        }
        else if (payment.Status is not (PaymentStatus.Pending or PaymentStatus.Failed))
        {
            return null;
        }
        else
        {
            payment.Method = PaymentMethod.VNPay;
            payment.Provider = "VNPay";
            payment.Status = PaymentStatus.Pending;
            payment.ExpiresAt = now.AddHours(48);
            payment.UpdatedAt = now;
            payment.UpdatedBy = customerId;
        }
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return payment;
    }

    public Task<Payment?> GetPaymentByNumberAsync(string paymentNumber, CancellationToken cancellationToken = default) =>
        context.Payments.AsNoTracking().FirstOrDefaultAsync(payment => payment.PaymentNumber == paymentNumber &&
            !payment.IsDeleted, cancellationToken);

    public async Task<Payment?> ConfirmVnPayPaymentAsync(string paymentNumber,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var payment = await context.Payments.FromSqlInterpolated(
                $"SELECT * FROM payments WHERE payment_number = {paymentNumber} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (payment is null || payment.IsDeleted || payment.Method != PaymentMethod.VNPay)
            return null;
        if (payment.Status == PaymentStatus.Completed)
        {
            await transaction.CommitAsync(cancellationToken);
            return payment;
        }
        if (payment.Status is not (PaymentStatus.Pending or PaymentStatus.Processing)) return null;
        var now = clock.UtcNow;
        payment.Status = PaymentStatus.Completed;
        payment.PaidAt = now;
        payment.UpdatedAt = now;
        if (payment.ContractId is Guid contractId && payment.MilestoneId is Guid milestoneId)
        {
            var contract = await context.Contracts.FirstOrDefaultAsync(item => item.Id == contractId && !item.IsDeleted,
                cancellationToken);
            var milestone = await context.ContractMilestones.FirstOrDefaultAsync(item => item.Id == milestoneId &&
                item.ContractId == contractId && !item.IsDeleted, cancellationToken);
            if (contract is null || milestone is null) return null;
            milestone.Status = MilestoneStatus.Paid;
            milestone.CompletionDate = DateOnly.FromDateTime(now);
            milestone.UpdatedAt = now;
            if (milestone.IsDeposit && contract.Status == ContractStatus.PendingDeposit)
            {
                contract.Status = ContractStatus.Active;
                contract.DepositPaidAt = now;
                contract.UpdatedAt = now;
                var nextMilestone = await context.ContractMilestones
                    .FirstOrDefaultAsync(m => m.ContractId == contract.Id && m.SequenceNumber == 2 && m.Status == MilestoneStatus.Pending && !m.IsDeleted, cancellationToken);
                if (nextMilestone != null)
                {
                    nextMilestone.Status = MilestoneStatus.InProgress;
                    nextMilestone.UpdatedAt = now;
                }
                context.Notifications.Add(new Notification
                {
                    UserId = contract.CustomerId, Type = "contract.active", Title = "Contract is active",
                    Content = "The VNPay deposit was confirmed and the contract is now active.",
                    ReferenceType = "contract", ReferenceId = contract.Id
                });
                var supplierUserId = await context.SupplierProfiles.Where(profile => profile.Id == contract.SupplierId)
                    .Select(profile => profile.UserId).FirstOrDefaultAsync(cancellationToken);
                if (supplierUserId != Guid.Empty)
                    context.Notifications.Add(new Notification
                    {
                        UserId = supplierUserId, Type = "contract.active", Title = "Contract is active",
                        Content = "The VNPay deposit was confirmed and production can begin.",
                        ReferenceType = "contract", ReferenceId = contract.Id
                    });
            }
        }
        if (payment.OrderId is Guid orderId)
        {
            var order = await context.Orders.FirstOrDefaultAsync(item => item.Id == orderId && !item.IsDeleted, cancellationToken);
            if (order is not null)
            {
                order.Status = OrderStatus.Processing;
                order.UpdatedAt = now;
                context.Notifications.Add(new Notification
                {
                    UserId = order.CustomerId, Type = "order.paid", Title = "Order payment confirmed",
                    Content = $"Payment for order {order.OrderNumber} was confirmed via VNPay.",
                    ReferenceType = "order", ReferenceId = order.Id
                });
                var supplierUserId = await (from o in context.Orders
                                            join shop in context.Shops on o.ShopId equals shop.Id
                                            join profile in context.SupplierProfiles on shop.SupplierId equals profile.Id
                                            where o.Id == orderId
                                            select profile.UserId).FirstOrDefaultAsync(cancellationToken);
                if (supplierUserId != Guid.Empty)
                    context.Notifications.Add(new Notification
                    {
                        UserId = supplierUserId, Type = "order.paid", Title = "New paid order",
                        Content = $"Payment for order {order.OrderNumber} has been received. Please process for shipment.",
                        ReferenceType = "order", ReferenceId = order.Id
                    });
            }
        }
        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "Payment", AggregateId = payment.Id, EventType = "VnPayPaymentConfirmed",
            ToStatus = PaymentStatus.Completed.ToString(), Reason = "VNPay IPN confirmed the payment."
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return payment;
    }

    public async Task<bool> FailVnPayPaymentAsync(string paymentNumber, string reason,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var payment = await context.Payments.FromSqlInterpolated(
                $"SELECT * FROM payments WHERE payment_number = {paymentNumber} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (payment is null || payment.IsDeleted || payment.Method != PaymentMethod.VNPay)
            return false;
        if (payment.Status == PaymentStatus.Completed)
        {
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        if (payment.Status is not (PaymentStatus.Pending or PaymentStatus.Processing or PaymentStatus.Failed))
            return false;
        var previousStatus = payment.Status;
        payment.Status = PaymentStatus.Failed;
        payment.UpdatedAt = clock.UtcNow;
        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "Payment", AggregateId = payment.Id, EventType = "VnPayPaymentFailed",
            FromStatus = previousStatus.ToString(), ToStatus = PaymentStatus.Failed.ToString(), Reason = reason
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<Payment?> AttachEvidenceAsync(Guid paymentId, Guid customerId, string evidenceUrl, string? notes, CancellationToken cancellationToken = default)
    {
        var payment = await context.Payments.FirstOrDefaultAsync(
            p => p.Id == paymentId && p.CustomerId == customerId && !p.IsDeleted, cancellationToken);
        if (payment is null) return null;
        payment.EvidenceUrl = evidenceUrl.Trim();
        payment.EvidenceNotes = notes?.Trim();
        payment.Status = PaymentStatus.Processing;
        payment.UpdatedAt = clock.UtcNow;
        payment.UpdatedBy = customerId;
        await context.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task<EscrowSummaryResponse> GetEscrowSummaryAsync(CancellationToken cancellationToken = default)
    {
        var contracts = await context.Contracts
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var contractIds = contracts.Select(c => c.Id).ToList();
        var allMilestones = await context.ContractMilestones
            .AsNoTracking()
            .Where(m => contractIds.Contains(m.ContractId) && !m.IsDeleted)
            .ToListAsync(cancellationToken);

        var milestonesByContract = allMilestones
            .GroupBy(m => m.ContractId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var customerIds = contracts.Select(c => c.CustomerId).Distinct().ToList();
        var supplierIds = contracts.Select(c => c.SupplierId).Distinct().ToList();

        var customers = await context.CustomerProfiles.AsNoTracking()
            .Where(cp => customerIds.Contains(cp.UserId))
            .ToDictionaryAsync(cp => cp.UserId, cp => cp.CompanyName, cancellationToken);

        var suppliers = await context.SupplierProfiles.AsNoTracking()
            .Where(sp => supplierIds.Contains(sp.Id))
            .ToDictionaryAsync(sp => sp.Id, sp => sp.CompanyName, cancellationToken);

        var ledgerItems = new List<EscrowLedgerItemDto>();
        decimal totalEscrow = 0m;
        int activeCount = 0;
        decimal pendingDisbursement = 0m;
        decimal disputedAmount = 0m;

        foreach (var c in contracts)
        {
            var milestones = milestonesByContract.GetValueOrDefault(c.Id, new List<ContractMilestone>());
            var paidMilestones = milestones.Where(m => m.Status == MilestoneStatus.Paid).ToList();
            var heldAmount = paidMilestones.Sum(m => m.Amount ?? 0m);

            if (c.Status == ContractStatus.Active) activeCount++;
            if (c.Status is ContractStatus.Active or ContractStatus.PendingDeposit)
            {
                totalEscrow += heldAmount;
            }
            if (c.Status is ContractStatus.CompletedOnTime or ContractStatus.CompletedLate)
            {
                pendingDisbursement += heldAmount;
            }
            if (c.Status is ContractStatus.Breached or ContractStatus.Disputed)
            {
                disputedAmount += heldAmount;
            }

            var customerName = (customers.TryGetValue(c.CustomerId, out var cName) && !string.IsNullOrWhiteSpace(cName)) ? cName : "Doanh nghiệp B2B";
            var supplierName = (suppliers.TryGetValue(c.SupplierId, out var sName) && !string.IsNullOrWhiteSpace(sName)) ? sName : "Nhà cung cấp";

            ledgerItems.Add(new EscrowLedgerItemDto(
                c.Id,
                c.ContractNumber,
                customerName,
                supplierName,
                c.TotalValue ?? 0m,
                heldAmount,
                paidMilestones.Count,
                milestones.Count,
                c.Status.ToString(),
                c.DepositPaidAt
            ));
        }

        return new EscrowSummaryResponse(
            totalEscrow,
            activeCount,
            pendingDisbursement,
            disputedAmount,
            ledgerItems
        );
    }

    private async Task NotifyPaymentPartiesAsync(Payment payment, string type, string title, string content, CancellationToken cancellationToken)
    {
        context.Notifications.Add(new Notification
        {
            UserId = payment.CustomerId, Type = type, Title = title, Content = content,
            ReferenceType = "payment", ReferenceId = payment.Id
        });
        if (payment.OrderId is Guid orderId)
        {
            var supplierUserId = await (from order in context.Orders
                                        join shop in context.Shops on order.ShopId equals shop.Id
                                        join profile in context.SupplierProfiles on shop.SupplierId equals profile.Id
                                        where order.Id == orderId
                                        select profile.UserId).FirstOrDefaultAsync(cancellationToken);
            if (supplierUserId != Guid.Empty)
                context.Notifications.Add(new Notification { UserId = supplierUserId, Type = type, Title = title, Content = content, ReferenceType = "payment", ReferenceId = payment.Id });
        }
        if (payment.ContractId is Guid contractId)
        {
            var contract = await context.Contracts.FirstOrDefaultAsync(item => item.Id == contractId, cancellationToken);
            if (contract is not null) await NotifyContractPartiesAsync(contract, type, title, content, cancellationToken);
        }
    }

    private async Task NotifyContractPartiesAsync(Contract contract, string type, string title, string content, CancellationToken cancellationToken)
    {
        var supplierUserId = await context.SupplierProfiles.Where(profile => profile.Id == contract.SupplierId)
            .Select(profile => profile.UserId).FirstOrDefaultAsync(cancellationToken);
        context.Notifications.AddRange(
            new Notification { UserId = contract.CustomerId, Type = type, Title = title, Content = content, ReferenceType = "contract", ReferenceId = contract.Id },
            new Notification { UserId = supplierUserId, Type = type, Title = title, Content = content, ReferenceType = "contract", ReferenceId = contract.Id });
    }
}
