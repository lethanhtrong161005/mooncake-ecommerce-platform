namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Globalization;
using System.Text.Json;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed class PaymentService(IPaymentRepository repository, IAdminPlatformRepository adminRepository, IDateTimeProvider clock) : IPaymentService
{
    private const string CodLimitConfigKey = "payments.cod_limit_vnd";
    private const string PaymentDeadlineConfigKey = "payments.payment_deadline_hours";

    public async Task<PaymentResponse> CreateAsync(Guid customerId, string? idempotencyKey, CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var key = idempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(key) || key.Length is < 8 or > 128)
            throw new HttpException(400, "Idempotency-Key is required and must contain 8 to 128 characters.");
        var isOrderPayment = request.OrderId.HasValue && !request.ContractId.HasValue && !request.MilestoneId.HasValue;
        var isMilestonePayment = !request.OrderId.HasValue && request.ContractId.HasValue && request.MilestoneId.HasValue;
        if (!isOrderPayment && !isMilestonePayment)
            throw new HttpException(400, "Provide either an order ID or both a contract ID and milestone ID.");
        if (!Enum.IsDefined(request.Method)) throw new HttpException(400, "Payment method is invalid.");

        var existingKey = await repository.GetPaymentByIdempotencyKeyAsync(customerId, key, cancellationToken);
        if (existingKey is not null)
        {
            if (existingKey.OrderId != request.OrderId || existingKey.ContractId != request.ContractId
                || existingKey.MilestoneId != request.MilestoneId || existingKey.Method != request.Method)
                throw new HttpException(409, "This Idempotency-Key was already used for a different payment.");
            return ToResponse(existingKey);
        }

        decimal amount;
        if (isOrderPayment)
        {
            var order = await repository.GetOrderForPaymentAsync(customerId, request.OrderId!.Value, cancellationToken)
                        ?? throw new HttpException(404, "Order was not found.");
            if (order.Status is OrderStatus.Cancelled or OrderStatus.Pending)
                throw new HttpException(409, "A payment cannot be created for an unconfirmed or cancelled order.");
            amount = order.TotalAmount;
            if (request.Method == PaymentMethod.Cash)
            {
                var limit = await GetDecimalConfigAsync(CodLimitConfigKey, cancellationToken);
                if (limit <= 0 || amount > limit)
                    throw new HttpException(422, "Cash on delivery is not enabled for this amount.");
            }
        }
        else
        {
            if (request.Method == PaymentMethod.Cash)
                throw new HttpException(422, "Cash on delivery is not available for contract milestones.");
            var target = await repository.GetMilestoneForPaymentAsync(customerId, request.ContractId!.Value, request.MilestoneId!.Value, cancellationToken);
            if (target.Contract is null || target.Milestone is null)
                throw new HttpException(404, "Payable contract milestone was not found.");
            if (target.Contract.Status is not (ContractStatus.PendingDeposit or ContractStatus.Active)
                || target.Milestone.Status is not (MilestoneStatus.AwaitingPayment or MilestoneStatus.CompletedOnTime or MilestoneStatus.CompletedLate))
                throw new HttpException(409, "This contract milestone is not ready for payment.");
            amount = target.Milestone.Amount ?? 0m;
        }
        if (amount <= 0) throw new HttpException(409, "The payable amount must be greater than zero.");

        var now = clock.UtcNow;
        var deadlineHours = await GetIntConfigAsync(PaymentDeadlineConfigKey, 48, cancellationToken);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            ContractId = request.ContractId,
            MilestoneId = request.MilestoneId,
            CustomerId = customerId,
            PaymentNumber = $"PAY-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant(),
            IdempotencyKey = key,
            Amount = amount,
            Currency = "VND",
            Method = request.Method,
            Status = PaymentStatus.Pending,
            ExpiresAt = now.AddHours(deadlineHours),
            CreatedBy = customerId
        };
        if (!await repository.CreatePaymentAsync(payment, cancellationToken))
        {
            existingKey = await repository.GetPaymentByIdempotencyKeyAsync(customerId, key, cancellationToken);
            if (existingKey is not null && existingKey.OrderId == request.OrderId && existingKey.ContractId == request.ContractId
                && existingKey.MilestoneId == request.MilestoneId && existingKey.Method == request.Method)
                return ToResponse(existingKey);
            throw new HttpException(409, "A payment is already in progress for this order or milestone.");
        }
        return ToResponse(payment);
    }

    public async Task<IReadOnlyList<PaymentResponse>> GetMineAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default) =>
        (await repository.GetPaymentsAsync(userId, supplier, cancellationToken)).Select(ToResponse).ToArray();

    public async Task<PaymentResponse> ConfirmAsync(Guid paymentId, Guid adminUserId, string reason, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var payment = await repository.ConfirmPaymentAsync(paymentId, adminUserId, reason.Trim(), ipAddress, cancellationToken)
                      ?? throw new HttpException(409, "Only a pending or processing payment can be confirmed.");
        return ToResponse(payment);
    }

    public async Task<PaymentResponse> RefundAsync(Guid paymentId, Guid adminUserId, string reason, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var payment = await repository.RefundPaymentAsync(paymentId, adminUserId, reason.Trim(), ipAddress, cancellationToken)
                      ?? throw new HttpException(409, "This payment cannot be refunded.");
        return ToResponse(payment);
    }

    private async Task<decimal> GetDecimalConfigAsync(string key, CancellationToken cancellationToken)
    {
        var value = await adminRepository.GetSystemConfigAsync(key, cancellationToken);
        if (value is null) return 0m;
        try { return JsonDocument.Parse(value.ValueJson).RootElement.GetDecimal(); }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException) { return 0m; }
    }

    private async Task<int> GetIntConfigAsync(string key, int fallback, CancellationToken cancellationToken)
    {
        var value = await adminRepository.GetSystemConfigAsync(key, cancellationToken);
        if (value is null) return fallback;
        try
        {
            var parsed = JsonDocument.Parse(value.ValueJson).RootElement.GetInt32();
            return parsed is > 0 and <= 720 ? parsed : fallback;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException) { return fallback; }
    }

    private static PaymentResponse ToResponse(Payment payment) => new(payment.Id, payment.PaymentNumber, payment.OrderId,
        payment.ContractId, payment.MilestoneId, payment.Amount, payment.Currency, payment.Method, payment.Status, payment.PaidAt, payment.ExpiresAt);
}
