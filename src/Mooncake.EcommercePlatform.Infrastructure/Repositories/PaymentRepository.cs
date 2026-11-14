namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

public sealed class PaymentRepository(ApplicationDbContext context, IDateTimeProvider clock) : IPaymentRepository
{
    public async Task<PagedResult<Payment>> GetPaymentsPageAsync(Guid userId, bool supplier,
        PaymentPageQueryRequest query, CancellationToken cancellationToken = default)
    {
        IQueryable<Payment> rows = context.Payments.AsNoTracking().Where(payment => !payment.IsDeleted);
        if (supplier)
        {
            rows = rows.Where(payment =>
                payment.OrderId.HasValue && context.Orders.Any(order => order.Id == payment.OrderId.Value
                    && !order.IsDeleted && context.Shops.Any(shop => shop.Id == order.ShopId && !shop.IsDeleted
                        && (shop.OwnerUserId == userId || context.SupplierProfiles.Any(profile =>
                            profile.Id == shop.SupplierId && profile.UserId == userId && !profile.IsDeleted))))
                || payment.ContractId.HasValue && context.Contracts.Any(contract => contract.Id == payment.ContractId.Value
                    && !contract.IsDeleted && context.SupplierProfiles.Any(profile =>
                        profile.Id == contract.SupplierId && profile.UserId == userId && !profile.IsDeleted)));
        }
        else rows = rows.Where(payment => payment.CustomerId == userId);

        if (query.Status.HasValue) rows = rows.Where(payment => payment.Status == query.Status.Value);
        if (query.Method.HasValue) rows = rows.Where(payment => payment.Method == query.Method.Value);
        if (query.OrderId.HasValue) rows = rows.Where(payment => payment.OrderId == query.OrderId.Value);
        if (query.From.HasValue) rows = rows.Where(payment => payment.CreatedAt >= query.From.Value);
        if (query.To.HasValue) rows = rows.Where(payment => payment.CreatedAt <= query.To.Value);
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = $"%{query.Q.Trim()}%";
            rows = rows.Where(payment => EF.Functions.ILike(payment.PaymentNumber, term)
                || (payment.TransactionId != null && EF.Functions.ILike(payment.TransactionId, term)));
        }

        var totalCount = await rows.CountAsync(cancellationToken);
        rows = query.Sort switch
        {
            "createdAtAsc" => rows.OrderBy(payment => payment.CreatedAt).ThenBy(payment => payment.Id),
            "amountAsc" => rows.OrderBy(payment => payment.Amount).ThenBy(payment => payment.Id),
            "amountDesc" => rows.OrderByDescending(payment => payment.Amount).ThenByDescending(payment => payment.Id),
            _ => rows.OrderByDescending(payment => payment.CreatedAt).ThenByDescending(payment => payment.Id)
        };
        var items = await PagedQueryHelper.ApplyPaging(rows, query).ToListAsync(cancellationToken);
        return new PagedResult<Payment>(items, query.Page, query.PageSize, totalCount);
    }

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
                 where !payment.IsDeleted
                       && ((shop != null && !shop.IsDeleted && (shop.OwnerUserId == userId || (profile != null && profile.UserId == userId && !profile.IsDeleted)))
                           || (contractProfile != null && contractProfile.UserId == userId && !contractProfile.IsDeleted))
                 orderby payment.CreatedAt descending
                 select payment).Take(100).ToListAsync(cancellationToken)
        : await context.Payments.AsNoTracking().Where(payment => payment.CustomerId == userId && !payment.IsDeleted)
            .OrderByDescending(payment => payment.CreatedAt).Take(100).ToListAsync(cancellationToken);

    public async Task<bool> CreatePaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        if (payment.PartId.HasValue && await context.Payments.AnyAsync(existing => existing.PartId == payment.PartId
                && (existing.Status == PaymentStatus.Pending || existing.Status == PaymentStatus.Processing || existing.Status == PaymentStatus.Completed)
                && !existing.IsDeleted, cancellationToken)) return false;
        if (payment.OrderId.HasValue && !payment.PartId.HasValue && await context.Payments.AnyAsync(existing => existing.OrderId == payment.OrderId
                && existing.PartId == null
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
            ConstraintName: "payments_idempotency_key_key"
                or "payments_one_open_payment_per_order_key"
                or "payments_one_open_payment_per_part_key"
                or "payments_one_open_payment_per_order_legacy_key"
                or "payments_one_open_payment_per_milestone_key"
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
            if (payment.PartId is Guid partId)
            {
                var part = await context.OrderPaymentParts
                    .FirstOrDefaultAsync(item => item.Id == partId && item.Status == "Processing" && !item.IsDeleted, cancellationToken);
                if (part is not null)
                {
                    part.Status = "Pending";
                    part.UpdatedAt = now;
                }
            }
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

        if (payment.OrderId is not null)
        {
            await SettleOrderPartPaymentCoreAsync(payment, now, cancellationToken);
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
        if (await context.Payments.AnyAsync(p => p.ContractId == contractId && p.MilestoneId == null
                && (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Processing || p.Status == PaymentStatus.Completed)
                && !p.IsDeleted, cancellationToken))
            return null;
        var milestone = await context.ContractMilestones.FromSqlInterpolated(
                $"SELECT * FROM contract_milestones WHERE id = {milestoneId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (contract is null || milestone is null || contract.CustomerId != customerId ||
            milestone.ContractId != contractId ||
            (contract.Status != ContractStatus.PendingDeposit && contract.Status != ContractStatus.Active) ||
            milestone.Amount is not > 0)
            return null;

        if (!milestone.IsDeposit && milestone.SequenceNumber == 3)
        {
            var milestone2Paid = await context.ContractMilestones.AsNoTracking().AnyAsync(item =>
                item.ContractId == contractId && item.SequenceNumber == 2 && item.Status == MilestoneStatus.Paid
                && !item.IsDeleted, cancellationToken);
            var shipments = context.Deliveries.AsNoTracking()
                .Where(item => item.ContractId == contractId && !item.IsDeleted);
            var shipmentsDelivered = await shipments.AnyAsync(cancellationToken)
                && !await shipments.AnyAsync(item => item.Status != DeliveryStatus.Delivered, cancellationToken);
            if (!milestone2Paid || !shipmentsDelivered)
                return null;
        }

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

    public async Task<Payment?> PrepareFullContractVnPayPaymentAsync(Guid customerId, Guid contractId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var contract = await context.Contracts.FromSqlInterpolated(
                $"SELECT * FROM contracts WHERE id = {contractId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (contract is null || contract.CustomerId != customerId || contract.IsDeleted
            || contract.Status != ContractStatus.PendingDeposit || contract.TotalValue is not > 0)
            return null;

        var existing = await context.Payments.FromSqlInterpolated(
                $"SELECT * FROM payments WHERE contract_id = {contractId} AND milestone_id IS NULL AND is_deleted = FALSE FOR UPDATE")
            .OrderByDescending(p => p.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (existing?.Status == PaymentStatus.Completed) return null;
        var now = clock.UtcNow;
        if (existing is null)
        {
            existing = new Payment
            {
                Id = Guid.NewGuid(), ContractId = contractId, CustomerId = customerId,
                PaymentNumber = $"PAY-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant(),
                Amount = contract.TotalValue.Value, Currency = "VND", Method = PaymentMethod.VNPay,
                Provider = "VNPay", Status = PaymentStatus.Pending, ExpiresAt = now.AddHours(48), CreatedBy = customerId
            };
            context.Payments.Add(existing);
        }
        else if (existing.Status is PaymentStatus.Pending or PaymentStatus.Failed)
        {
            existing.Amount = contract.TotalValue.Value;
            existing.Method = PaymentMethod.VNPay;
            existing.Provider = "VNPay";
            existing.Status = PaymentStatus.Pending;
            existing.ExpiresAt = now.AddHours(48);
            existing.UpdatedAt = now;
            existing.UpdatedBy = customerId;
        }
        else return null;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return existing;
    }

    public async Task<Payment?> PrepareOrderVnPayPaymentAsync(Guid customerId, Guid orderId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var order = await context.Orders.FromSqlInterpolated(
                $"SELECT * FROM orders WHERE id = {orderId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null || order.CustomerId != customerId || order.IsDeleted ||
            order.Status is OrderStatus.Pending or OrderStatus.Cancelled or OrderStatus.Expired
                or OrderStatus.AwaitingCustomerDeliveryApproval || order.TotalAmount <= 0)
            return null;

        var now = clock.UtcNow;
        var scheduledPartIds = await (from part in context.OrderPaymentParts.AsNoTracking()
                                      join obligation in context.OrderPaymentObligations.AsNoTracking()
                                          on part.ObligationId equals obligation.Id
                                      where part.OrderId == orderId && !part.IsDeleted && !obligation.IsDeleted
                                            && obligation.Status != "Cancelled"
                                      orderby obligation.SequenceNo, part.PartIndex
                                      select new { part.Id, part.Status }).ToListAsync(cancellationToken);

        if (scheduledPartIds.Count > 0)
        {
            var nextOpen = scheduledPartIds.FirstOrDefault(item => item.Status is "Pending" or "Processing");
            if (nextOpen is null)
            {
                var latestCompleted = await context.Payments.AsNoTracking()
                    .Where(item => item.OrderId == orderId && item.Status == PaymentStatus.Completed && !item.IsDeleted)
                    .OrderByDescending(item => item.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return latestCompleted;
            }

            var lockedPart = await context.OrderPaymentParts.FromSqlInterpolated(
                    $"SELECT * FROM payment_parts WHERE id = {nextOpen.Id} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (lockedPart is null || lockedPart.IsDeleted || lockedPart.Status is "Cancelled" or "Refunded")
                return null;

            var partPayment = await context.Payments.FromSqlInterpolated(
                    $"SELECT * FROM payments WHERE part_id = {lockedPart.Id} AND is_deleted = FALSE FOR UPDATE")
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (lockedPart.Status == "Paid" || partPayment?.Status == PaymentStatus.Completed)
            {
                await transaction.CommitAsync(cancellationToken);
                return partPayment;
            }

            if (partPayment is null || partPayment.Status == PaymentStatus.Failed)
            {
                partPayment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    PartId = lockedPart.Id,
                    CustomerId = customerId,
                    PaymentNumber = $"PAY-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant(),
                    Amount = lockedPart.Amount,
                    Currency = "VND",
                    Method = PaymentMethod.VNPay,
                    Provider = "VNPay",
                    Status = PaymentStatus.Pending,
                    ExpiresAt = now.AddHours(OrderPolicyDefaults.DepositPaymentSlaHours),
                    CreatedBy = customerId,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                context.Payments.Add(partPayment);
            }
            else
            {
                partPayment.Method = PaymentMethod.VNPay;
                partPayment.Provider = "VNPay";
                partPayment.Status = PaymentStatus.Pending;
                partPayment.ExpiresAt = now.AddHours(OrderPolicyDefaults.DepositPaymentSlaHours);
                partPayment.UpdatedAt = now;
                partPayment.UpdatedBy = customerId;
            }

            lockedPart.Status = "Processing";
            lockedPart.UpdatedAt = now;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return partPayment;
        }

        var payment = await context.Payments.FromSqlInterpolated(
                $"SELECT * FROM payments WHERE order_id = {orderId} AND part_id IS NULL AND is_deleted = FALSE FOR UPDATE")
            .OrderByDescending(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);
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
            await transaction.CommitAsync(cancellationToken);
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
        if (payment.Status is not (PaymentStatus.Pending or PaymentStatus.Processing or PaymentStatus.Failed))
            return null;

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
                        Content = $"The VNPay deposit for contract {contract.ContractNumber} was confirmed. Please begin production and prepare the shipment according to the contract delivery schedule.",
                        ReferenceType = "contract", ReferenceId = contract.Id
                    });
            }
        }
        else if (payment.ContractId is Guid prepaidContractId)
        {
            var contract = await context.Contracts.FirstOrDefaultAsync(item => item.Id == prepaidContractId && !item.IsDeleted,
                cancellationToken);
            if (contract is null || contract.CustomerId != payment.CustomerId) return null;
            var deposit = await context.ContractMilestones.FirstOrDefaultAsync(m => m.ContractId == contract.Id
                && m.IsDeposit && !m.IsDeleted, cancellationToken);
            if (deposit is null) return null;
            var unusedDepositInstructions = await context.Payments
                .Where(p => p.ContractId == contract.Id && p.MilestoneId == deposit.Id
                    && p.Id != payment.Id && p.Status == PaymentStatus.Pending && !p.IsDeleted)
                .ToListAsync(cancellationToken);
            foreach (var unusedPayment in unusedDepositInstructions)
            {
                unusedPayment.IsDeleted = true;
                unusedPayment.UpdatedAt = now;
            }
            deposit.Status = MilestoneStatus.Paid;
            deposit.CompletionDate = DateOnly.FromDateTime(now);
            deposit.UpdatedAt = now;
            if (contract.Status == ContractStatus.PendingDeposit)
            {
                contract.Status = ContractStatus.Active;
                contract.DepositPaidAt = now;
                contract.UpdatedAt = now;
                var nextMilestone = await context.ContractMilestones.FirstOrDefaultAsync(m => m.ContractId == contract.Id
                    && m.SequenceNumber == 2 && m.Status == MilestoneStatus.Pending && !m.IsDeleted, cancellationToken);
                if (nextMilestone is not null)
                {
                    nextMilestone.Status = MilestoneStatus.InProgress;
                    nextMilestone.UpdatedAt = now;
                }
            }
            context.Notifications.Add(new Notification
            {
                UserId = contract.CustomerId, Type = "contract.active", Title = "Contract is active",
                Content = "Your full contract payment was confirmed and is held for release according to the accepted milestones.",
                ReferenceType = "contract", ReferenceId = contract.Id
            });
        }

        if (payment.OrderId is not null)
        {
            await SettleOrderPartPaymentCoreAsync(payment, now, cancellationToken);
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

    private async Task SettleOrderPartPaymentCoreAsync(Payment payment, DateTime now, CancellationToken cancellationToken)
    {
        if (payment.OrderId is not Guid orderId) return;

        var order = await context.Orders.FromSqlInterpolated(
                $"SELECT * FROM orders WHERE id = {orderId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null || order.IsDeleted) return;

        if (payment.PartId is Guid partId)
        {
            var lockedPart = await context.OrderPaymentParts.FromSqlInterpolated(
                    $"SELECT * FROM payment_parts WHERE id = {partId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (lockedPart is not null && !lockedPart.IsDeleted)
            {
                lockedPart.Status = "Paid";
                lockedPart.PaidAt = now;
                lockedPart.UpdatedAt = now;

                var lockedObligation = await context.OrderPaymentObligations.FromSqlInterpolated(
                        $"SELECT * FROM order_payment_schedule WHERE id = {lockedPart.ObligationId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken);
                if (lockedObligation is not null && !lockedObligation.IsDeleted)
                {
                    var siblingParts = await context.OrderPaymentParts.AsNoTracking()
                        .Where(item => item.ObligationId == lockedObligation.Id && item.Id != lockedPart.Id && !item.IsDeleted)
                        .ToListAsync(cancellationToken);
                    var totalPaidParts = lockedPart.Amount + siblingParts.Where(item => item.Status == "Paid").Sum(item => item.Amount);
                    lockedObligation.PaidAmount = Math.Min(lockedObligation.TotalAmount, totalPaidParts);
                    if (order.Status is not (OrderStatus.Cancelled or OrderStatus.Expired))
                    {
                        lockedObligation.Status = lockedObligation.PaidAmount >= lockedObligation.TotalAmount
                            ? "Paid"
                            : "PartiallyPaid";
                        if (lockedObligation.Status == "Paid")
                            lockedObligation.PaidAt ??= now;
                    }
                    lockedObligation.UpdatedAt = now;
                }
            }
        }

        var capturedKey = $"payment-captured:{payment.Id}";
        if (!await context.OrderFinancialLedgerEntries.AnyAsync(item => item.IdempotencyKey == capturedKey, cancellationToken))
        {
            context.OrderFinancialLedgerEntries.Add(new OrderFinancialLedgerEntry
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ShopId = order.ShopId,
                CustomerId = order.CustomerId,
                PaymentId = payment.Id,
                EntryType = "Captured",
                BalanceBucket = "Held",
                Amount = payment.Amount,
                Description = $"Payment captured {payment.Amount:N0} VND ({payment.PaymentNumber}) into platform Held balance.",
                IdempotencyKey = capturedKey,
                CreatedBy = payment.CustomerId,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Expired)
        {
            // Late IPN after cancellation or expiry: never revive the order; auto-open/increment a Pending refund case.
            var openRefundCase = await context.OrderRefundCases
                .FirstOrDefaultAsync(item => item.OrderId == order.Id && item.Status == "Pending" && !item.IsDeleted, cancellationToken);
            if (openRefundCase is null)
            {
                openRefundCase = new OrderRefundCase
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    CustomerId = order.CustomerId,
                    ShopId = order.ShopId,
                    CaseNumber = $"RFD-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
                    RequestedAmount = payment.Amount,
                    ApprovedAmount = 0m,
                    Reason = $"Late payment ({payment.PaymentNumber}) arrived after order {order.Status}; automatic refund queued.",
                    Status = "Pending",
                    SandboxSimulated = true,
                    CreatedBy = payment.CustomerId,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                context.OrderRefundCases.Add(openRefundCase);
            }
            else
            {
                openRefundCase.RequestedAmount += payment.Amount;
                openRefundCase.UpdatedAt = now;
            }

            var lateRefundKey = $"late-ipn-refund:{payment.Id}";
            if (!await context.OrderFinancialLedgerEntries.AnyAsync(item => item.IdempotencyKey == lateRefundKey, cancellationToken))
            {
                context.OrderFinancialLedgerEntries.Add(new OrderFinancialLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ShopId = order.ShopId,
                    CustomerId = order.CustomerId,
                    PaymentId = payment.Id,
                    RefundCaseId = openRefundCase.Id,
                    EntryType = "ReservedForRefund",
                    BalanceBucket = "Held",
                    Amount = 0m,
                    Description = $"Late payment ({payment.PaymentNumber}) flagged for automatic refund ({payment.Amount:N0} VND) on {order.Status} order.",
                    IdempotencyKey = lateRefundKey,
                    CreatedBy = payment.CustomerId,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            order.Version += 1;
            order.UpdatedAt = now;
            context.Notifications.Add(new Notification
            {
                UserId = order.CustomerId,
                Type = "order.late_payment_refund_queued",
                Title = "Late payment queued for refund",
                Content = $"Payment {payment.PaymentNumber} arrived after order {order.OrderNumber} was {order.Status.ToString().ToLowerInvariant()}. Refund case {openRefundCase.CaseNumber} has been opened automatically.",
                ReferenceType = "order",
                ReferenceId = order.Id
            });
        }
        else
        {
            order.Version += 1;
            order.UpdatedAt = now;

            // Update inventory reservation deadline if all pre-production obligations are now satisfied.
            var allObligations = await context.OrderPaymentObligations.AsNoTracking()
                .Where(item => item.OrderId == order.Id && !item.IsDeleted && item.Status != "Cancelled")
                .ToListAsync(cancellationToken);
            var currentUpdatedPartObligationId = payment.PartId.HasValue
                ? (await context.OrderPaymentParts.AsNoTracking()
                    .Where(item => item.Id == payment.PartId.Value)
                    .Select(item => (Guid?)item.ObligationId)
                    .FirstOrDefaultAsync(cancellationToken))
                : null;

            var preProdSatisfied = allObligations.Count == 0 || allObligations
                .Where(item => item.IsRequiredBeforeProduction)
                .All(item => item.Status == "Paid" ||
                    (currentUpdatedPartObligationId.HasValue && item.Id == currentUpdatedPartObligationId.Value &&
                     context.ChangeTracker.Entries<OrderPaymentObligation>()
                         .Any(e => e.Entity.Id == item.Id && e.Entity.Status == "Paid")));

            if (preProdSatisfied)
            {
                var customPackaging = await context.CustomPackagings.AsNoTracking()
                    .FirstOrDefaultAsync(item => item.OrderId == order.Id && !item.IsDeleted, cancellationToken);
                var awaitingMockup = customPackaging is not null &&
                    customPackaging.Status is not (CustomPackagingStatus.Approved or CustomPackagingStatus.InProduction or CustomPackagingStatus.Completed);

                if (awaitingMockup)
                {
                    var mockupExpiry = BusinessCalendarHelper.AddBusinessDays(now, OrderPolicyDefaults.MockupReservationBusinessDays);
                    order.ReservationExpiresAt = mockupExpiry;
                    await context.InventoryReservations
                        .Where(item => item.OrderId == order.Id && item.Status == "Active")
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(item => item.ExpiresAt, mockupExpiry)
                            .SetProperty(item => item.UpdatedAt, now), cancellationToken);
                }
                else
                {
                    order.ReservationExpiresAt = null;
                    await context.InventoryReservations
                        .Where(item => item.OrderId == order.Id && item.Status == "Active")
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(item => item.Status, "Committed")
                            .SetProperty(item => item.ExpiresAt, (DateTime?)null)
                            .SetProperty(item => item.UpdatedAt, now), cancellationToken);
                }
            }

            context.Notifications.Add(new Notification
            {
                UserId = order.CustomerId, Type = "order.paid", Title = "Order payment confirmed",
                Content = $"Payment {payment.PaymentNumber} ({payment.Amount:N0} VND) for order {order.OrderNumber} was confirmed.",
                ReferenceType = "order", ReferenceId = order.Id
            });
            var supplierUserId = await (from o in context.Orders
                                        join shop in context.Shops on o.ShopId equals shop.Id
                                        join profile in context.SupplierProfiles on shop.SupplierId equals profile.Id
                                        where o.Id == orderId
                                        select profile.UserId).FirstOrDefaultAsync(cancellationToken);
            if (supplierUserId != Guid.Empty)
            {
                context.Notifications.Add(new Notification
                {
                    UserId = supplierUserId, Type = "order.paid", Title = "Order payment received",
                    Content = $"Payment {payment.PaymentNumber} ({payment.Amount:N0} VND) for order {order.OrderNumber} has been received into platform Held balance.",
                    ReferenceType = "order", ReferenceId = order.Id
                });
            }
        }
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

        var now = clock.UtcNow;
        var previousStatus = payment.Status;
        payment.Status = PaymentStatus.Failed;
        payment.UpdatedAt = now;
        if (payment.PartId is Guid partId)
        {
            var lockedPart = await context.OrderPaymentParts
                .FirstOrDefaultAsync(item => item.Id == partId && item.Status == "Processing" && !item.IsDeleted, cancellationToken);
            if (lockedPart is not null)
            {
                lockedPart.Status = "Pending";
                lockedPart.UpdatedAt = now;
            }
        }

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
        var fullPrepayments = await context.Payments.AsNoTracking()
            .Where(p => contractIds.Contains(p.ContractId ?? Guid.Empty) && p.MilestoneId == null
                && p.Status == PaymentStatus.Completed && !p.IsDeleted)
            .GroupBy(p => p.ContractId!.Value)
            .Select(g => new { ContractId = g.Key, Amount = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.ContractId, x => x.Amount, cancellationToken);

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
            var hasFullPrepayment = fullPrepayments.TryGetValue(c.Id, out var fullPrepaidAmount);
            var heldAmount = hasFullPrepayment
                ? fullPrepaidAmount
                : paidMilestones.Sum(m => m.Amount ?? 0m);

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

            var customerName = (customers.TryGetValue(c.CustomerId, out var cName) && !string.IsNullOrWhiteSpace(cName)) ? cName : "Corporate Customer";
            var supplierName = (suppliers.TryGetValue(c.SupplierId, out var sName) && !string.IsNullOrWhiteSpace(sName)) ? sName : "Verified Supplier";

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
