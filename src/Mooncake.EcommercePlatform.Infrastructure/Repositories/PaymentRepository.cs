namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
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

    public Task<Payment?> GetPaymentByIdempotentReferenceAsync(string reference, CancellationToken cancellationToken = default) =>
        Guid.TryParseExact(reference, "N", out var paymentId)
            ? context.Payments.AsNoTracking().FirstOrDefaultAsync(payment => payment.Id == paymentId && !payment.IsDeleted, cancellationToken)
            : Task.FromResult<Payment?>(null);

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

    public async Task<VnPaySettlementResult> ProcessVnPayResultAsync(VnPayCallback callback,
        CancellationToken cancellationToken = default)
    {
        const string AcknowledgedCode = "00";
        const string AlreadyProcessedCode = "02";
        const string InvalidAmountCode = "04";
        const string UnknownPaymentCode = "01";
        const string SuccessMessage = "Payment status updated successfully.";
        const string AlreadyProcessedMessage = "Payment status was already recorded.";
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var payment = await context.Payments.FromSqlInterpolated(
                $"SELECT * FROM payments WHERE id = {Guid.ParseExact(callback.PaymentNumber, "N")} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        if (payment is null || payment.IsDeleted || payment.Provider != "VNPAY" || payment.Method != PaymentMethod.VNPay)
            return new VnPaySettlementResult(UnknownPaymentCode, "Payment was not found.");
        if (decimal.ToInt64(payment.Amount * 100m) != callback.Amount)
            return new VnPaySettlementResult(InvalidAmountCode, "Payment amount does not match.");
        if (payment.Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
        {
            var response = payment.Status == PaymentStatus.Completed && payment.TransactionId == callback.TransactionId
                ? new VnPaySettlementResult(AcknowledgedCode, AlreadyProcessedMessage)
                : new VnPaySettlementResult(AlreadyProcessedCode, AlreadyProcessedMessage);
            return response;
        }

        var now = clock.UtcNow;
        var previousStatus = payment.Status;
        var succeeded = callback.ResponseCode == "00" && callback.TransactionStatus == "00";
        payment.Status = succeeded ? PaymentStatus.Completed : PaymentStatus.Failed;
        payment.TransactionId = callback.TransactionId;
        payment.PaidAt = succeeded ? now : null;
        payment.UpdatedAt = now;
        if (succeeded && payment.MilestoneId is Guid milestoneId && payment.ContractId is Guid contractId)
        {
            var milestone = await context.ContractMilestones.FirstOrDefaultAsync(item =>
                item.Id == milestoneId && item.ContractId == contractId && !item.IsDeleted, cancellationToken);
            var contract = await context.Contracts.FirstOrDefaultAsync(item => item.Id == contractId && !item.IsDeleted, cancellationToken);
            if (milestone is null || contract is null)
                return new VnPaySettlementResult(UnknownPaymentCode, "Contract milestone was not found.");
            milestone.Status = MilestoneStatus.Paid;
            milestone.UpdatedAt = now;
            if (milestone.IsDeposit && contract.Status == ContractStatus.PendingDeposit)
            {
                contract.Status = ContractStatus.Active;
                contract.DepositPaidAt = now;
                contract.UpdatedAt = now;
                context.WorkflowEvents.Add(new WorkflowEvent
                {
                    AggregateType = "Contract", AggregateId = contract.Id, EventType = "ContractStatusChanged",
                    FromStatus = ContractStatus.PendingDeposit.ToString(), ToStatus = ContractStatus.Active.ToString(),
                    Reason = "Mandatory deposit payment confirmed by VNPAY."
                });
                await NotifyContractPartiesAsync(contract, "contract.active", "Contract is active",
                    "The required deposit was confirmed and the contract is now active.", cancellationToken);
            }
        }

        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "Payment", AggregateId = payment.Id, EventType = "PaymentStatusChanged",
            FromStatus = previousStatus.ToString(), ToStatus = payment.Status.ToString(),
            Reason = succeeded ? "VNPAY payment confirmed." : $"VNPAY payment failed with response {callback.ResponseCode}."
        });
        await NotifyPaymentPartiesAsync(payment, succeeded ? "payment.completed" : "payment.failed",
            succeeded ? "Payment confirmed" : "Payment failed",
            succeeded ? $"Payment {payment.PaymentNumber} was confirmed by VNPAY."
                : $"Payment {payment.PaymentNumber} failed at VNPAY.", cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new VnPaySettlementResult(AcknowledgedCode, SuccessMessage);
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
