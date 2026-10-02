namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements payment processing, anti-collision idempotency checks, milestone settlement, and refunds.</summary>
public class PaymentService(
    IPaymentRepository paymentRepository,
    IOrderRepository orderRepository,
    IContractRepository contractRepository,
    IPaymentHelper paymentHelper) : IPaymentService
{
    public async Task<PaymentResponse> ProcessPaymentAsync(PayRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Target validation (either Order OR Milestone, not both, not neither)
        if (request.OrderId.HasValue && request.MilestoneId.HasValue)
        {
            throw new HttpException(400, "Payment must be associated with either an Order OR a Contract Milestone, not both.");
        }

        if (!request.OrderId.HasValue && !request.MilestoneId.HasValue)
        {
            throw new HttpException(400, "Payment target must specify either OrderId or MilestoneId.");
        }

        // 2. Anti-collision idempotency check: prevent double charges on duplicate transaction refs
        if (!string.IsNullOrWhiteSpace(request.TransactionRef))
        {
            var existing = await paymentRepository.GetByTransactionRefAsync(request.TransactionRef.Trim(), cancellationToken);
            if (existing is { Status: PaymentStatus.Succeeded })
            {
                return paymentHelper.ToResponse(existing);
            }
        }

        var now = DateTime.UtcNow;
        var txnRef = !string.IsNullOrWhiteSpace(request.TransactionRef)
            ? request.TransactionRef.Trim()
            : $"TXN-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        // 3. Process Order target
        if (request.OrderId.HasValue)
        {
            var order = await orderRepository.GetByIdAsync(request.OrderId.Value, cancellationToken)
                ?? throw new HttpException(404, $"Order with ID {request.OrderId.Value} was not found.");

            if (order.Status == OrderStatus.Cancelled)
            {
                throw new HttpException(400, "Cannot process payment for a cancelled order.");
            }

            if (request.PaymentType == PaymentType.Deposit)
            {
                order.Status = OrderStatus.Confirmed;
            }
            else
            {
                order.Status = OrderStatus.Confirmed;
            }

            order.UpdatedAtUtc = now;
            await orderRepository.UpdateOrderAsync(order, cancellationToken);
        }

        // 4. Process Contract Milestone target
        if (request.MilestoneId.HasValue)
        {
            var milestone = await contractRepository.GetMilestoneByIdAsync(request.MilestoneId.Value, cancellationToken)
                ?? throw new HttpException(404, $"Contract milestone with ID {request.MilestoneId.Value} was not found.");

            if (milestone.Status == MilestoneStatus.Paid)
            {
                throw new HttpException(400, "Milestone has already been paid.");
            }

            milestone.Status = MilestoneStatus.Paid;
            milestone.PaidAtUtc = now;
            milestone.UpdatedAtUtc = now;
            await contractRepository.UpdateMilestoneAsync(milestone, cancellationToken);
        }

        // 5. Create Payment record (Succeeded)
        var payment = new Payment
        {
            OrderId = request.OrderId,
            MilestoneId = request.MilestoneId,
            PaymentType = request.PaymentType,
            Amount = request.Amount,
            Method = request.Method,
            Status = PaymentStatus.Succeeded,
            TransactionRef = txnRef,
            PaidAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var created = await paymentRepository.CreateAsync(payment, cancellationToken);
        var loaded = await paymentRepository.GetByIdAsync(created.Id, cancellationToken);

        return paymentHelper.ToResponse(loaded ?? created);
    }

    public async Task<PaymentResponse> RefundPaymentAsync(RefundRequest request, CancellationToken cancellationToken = default)
    {
        var original = await paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken)
            ?? throw new HttpException(404, $"Payment with ID {request.PaymentId} was not found.");

        if (original.Status != PaymentStatus.Succeeded)
        {
            throw new HttpException(400, "Only successful payments can be refunded.");
        }

        var now = DateTime.UtcNow;

        original.Status = PaymentStatus.Refunded;
        original.UpdatedAtUtc = now;
        await paymentRepository.UpdateAsync(original, cancellationToken);

        var refundRecord = new Payment
        {
            OrderId = original.OrderId,
            MilestoneId = original.MilestoneId,
            PaymentType = PaymentType.Refund,
            Amount = request.Amount,
            Method = original.Method,
            Status = PaymentStatus.Refunded,
            TransactionRef = $"REF-{original.TransactionRef}",
            PaidAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var created = await paymentRepository.CreateAsync(refundRecord, cancellationToken);
        var loaded = await paymentRepository.GetByIdAsync(created.Id, cancellationToken);

        return paymentHelper.ToResponse(loaded ?? created);
    }

    public async Task<IEnumerable<PaymentResponse>> GetPaymentsAsync(
        long? orderId = null,
        long? milestoneId = null,
        CancellationToken cancellationToken = default)
    {
        var payments = await paymentRepository.GetPaymentsAsync(orderId, milestoneId, cancellationToken);
        return payments.Select(paymentHelper.ToResponse);
    }

    public async Task<PaymentResponse?> GetPaymentByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var payment = await paymentRepository.GetByIdAsync(id, cancellationToken);
        return payment == null ? null : paymentHelper.ToResponse(payment);
    }
}
