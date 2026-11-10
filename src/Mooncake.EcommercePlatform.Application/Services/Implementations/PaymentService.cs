namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Globalization;
using System.Text.Json;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements order and contract payment orchestration, VNPay verification, and reconciliation.</summary>
public sealed class PaymentService(IPaymentRepository repository, IAdminPlatformRepository adminRepository,
    IVnPayGateway vnPayGateway, IDateTimeProvider clock, INotificationService notificationService) : IPaymentService
{
    private const string CodLimitConfigKey = "payments.cod_limit_vnd";
    private const string PaymentDeadlineConfigKey = "payments.payment_deadline_hours";

    public async Task<PagedResult<PaymentResponse>> GetMinePageAsync(Guid userId, bool supplier,
        PaymentPageQueryRequest query, CancellationToken cancellationToken = default)
    {
        PagedQueryHelper.Validate(query);
        PagedQueryHelper.ValidateSort(query.Sort, "createdAtDesc", "createdAtAsc", "amountAsc", "amountDesc");
        if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value)
            || query.Method.HasValue && !Enum.IsDefined(query.Method.Value))
            throw new HttpException(400, "Invalid payment filter.", "VALIDATION_ERROR");
        var page = await repository.GetPaymentsPageAsync(userId, supplier, query, cancellationToken);
        return new PagedResult<PaymentResponse>(page.Items.Select(ToResponse).ToArray(),
            page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<PaymentResponse> CreateAsync(Guid customerId, string? idempotencyKey, CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var key = idempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(key) || key.Length is < 8 or > 128)
            throw new HttpException(400, "Idempotency-Key is required and must contain 8 to 128 characters.", OrderErrorCodes.ValidationError);
        var isOrderPayment = request.OrderId.HasValue && !request.ContractId.HasValue && !request.MilestoneId.HasValue;
        var isMilestonePayment = !request.OrderId.HasValue && request.ContractId.HasValue && request.MilestoneId.HasValue;
        if (!isOrderPayment && !isMilestonePayment)
            throw new HttpException(400, "Provide either an order ID or both a contract ID and milestone ID.", OrderErrorCodes.ValidationError);
        if (!Enum.IsDefined(request.Method))
            throw new HttpException(400, "Payment method is invalid.", OrderErrorCodes.ValidationError);

        var existingKey = await repository.GetPaymentByIdempotencyKeyAsync(customerId, key, cancellationToken);
        if (existingKey is not null)
        {
            if (existingKey.OrderId != request.OrderId || existingKey.ContractId != request.ContractId
                || existingKey.MilestoneId != request.MilestoneId || existingKey.Method != request.Method)
                throw new HttpException(409, "This Idempotency-Key was already used for a different payment.", OrderErrorCodes.IdempotencyConflict);
            return ToResponse(existingKey);
        }

        decimal amount;
        if (isOrderPayment)
        {
            var order = await repository.GetOrderForPaymentAsync(customerId, request.OrderId!.Value, cancellationToken)
                        ?? throw new HttpException(404, B2BOrderingMessages.OrderNotFound);
            if (order.Status is OrderStatus.Cancelled or OrderStatus.Pending)
                throw new HttpException(409, "A payment cannot be created for an unconfirmed or cancelled order.", OrderErrorCodes.StateConflict);
            amount = order.TotalAmount;

            if (request.Method == PaymentMethod.BankTransfer)
            {
                var bankTransferEnabled = await GetBoolConfigAsync(
                    OrderPolicyDefaults.DirectOrderBankTransferEnabledKey,
                    OrderPolicyDefaults.DefaultDirectOrderBankTransferEnabled,
                    cancellationToken);
                if (!bankTransferEnabled)
                    throw new HttpException(422, "Direct bank transfer is disabled for direct bulk orders. Please pay via VNPay.", OrderErrorCodes.PaymentMethodNotSupported);
            }
            else if (request.Method == PaymentMethod.Cash)
            {
                if (order.PackagingFee > 0)
                    throw new HttpException(422, "Cash on delivery is not available for orders with custom packaging.", OrderErrorCodes.PaymentMethodNotSupported);
                var codEnabled = await GetBoolConfigAsync(
                    OrderPolicyDefaults.DirectOrderCodEnabledKey,
                    OrderPolicyDefaults.DefaultDirectOrderCodEnabled,
                    cancellationToken);
                if (!codEnabled)
                    throw new HttpException(422, "Cash on delivery is disabled for direct bulk orders. Please pay via VNPay.", OrderErrorCodes.PaymentMethodNotSupported);
                var limit = await GetDecimalConfigAsync(CodLimitConfigKey, cancellationToken);
                if (limit <= 0) limit = 10000000m;
                if (amount > limit)
                    throw new HttpException(422, "Cash on delivery is not enabled for this amount.", OrderErrorCodes.PaymentMethodNotSupported);
            }
        }
        else
        {
            if (request.Method == PaymentMethod.Cash)
                throw new HttpException(422, "Cash on delivery is not available for contract milestones.", OrderErrorCodes.PaymentMethodNotSupported);
            var target = await repository.GetMilestoneForPaymentAsync(customerId, request.ContractId!.Value, request.MilestoneId!.Value, cancellationToken);
            if (target.Contract is null || target.Milestone is null)
                throw new HttpException(404, "Payable contract milestone was not found.");
            if (target.Contract.Status is not (ContractStatus.PendingDeposit or ContractStatus.Active)
                || target.Milestone.Status is not (MilestoneStatus.AwaitingPayment or MilestoneStatus.CompletedOnTime or MilestoneStatus.CompletedLate))
                throw new HttpException(409, "This contract milestone is not ready for payment.", OrderErrorCodes.StateConflict);
            amount = target.Milestone.Amount ?? 0m;
        }
        if (amount <= 0)
            throw new HttpException(409, "The payable amount must be greater than zero.", OrderErrorCodes.StateConflict);

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
            throw new HttpException(409, "A payment is already in progress for this order or milestone.", OrderErrorCodes.StateConflict);
        }
        return ToResponse(payment);
    }

    public async Task<IReadOnlyList<PaymentResponse>> GetMineAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default) =>
        (await repository.GetPaymentsAsync(userId, supplier, cancellationToken)).Select(ToResponse).ToArray();

    public async Task<VnPayUrlResponse> CreateVnPayUrlAsync(Guid customerId, CreateVnPayUrlRequest request,
        string? clientIpAddress, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(request.ReturnUrl, UriKind.Absolute, out var returnUri) ||
            returnUri.Scheme != Uri.UriSchemeHttps && returnUri.Scheme != Uri.UriSchemeHttp)
            throw new HttpException(400, "A valid return URL is required.", OrderErrorCodes.ValidationError);
        var payment = await repository.PrepareVnPayPaymentAsync(customerId, request.ContractId,
            request.MilestoneId, cancellationToken)
            ?? throw new HttpException(409, "The contract deposit is not ready for VNPay payment.", OrderErrorCodes.StateConflict);
        if (payment.Status == PaymentStatus.Completed)
            throw new HttpException(409, "This contract deposit has already been paid.", OrderErrorCodes.StateConflict);
        return new VnPayUrlResponse(payment.Id, payment.PaymentNumber, payment.Amount,
            vnPayGateway.CreatePaymentUrl(payment, returnUri.AbsoluteUri, clientIpAddress ?? "127.0.0.1",
                clock.UtcNow));
    }

    public async Task<VnPayUrlResponse> CreateOrderVnPayUrlAsync(Guid customerId, CreateOrderVnPayUrlRequest request,
        string? clientIpAddress, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(request.ReturnUrl, UriKind.Absolute, out var returnUri) ||
            returnUri.Scheme != Uri.UriSchemeHttps && returnUri.Scheme != Uri.UriSchemeHttp)
            throw new HttpException(400, "A valid return URL is required.", OrderErrorCodes.ValidationError);
        var payment = await repository.PrepareOrderVnPayPaymentAsync(customerId, request.OrderId, cancellationToken)
            ?? throw new HttpException(409, "The order is not ready for VNPay payment.", OrderErrorCodes.StateConflict);
        if (payment.Status == PaymentStatus.Completed)
            throw new HttpException(409, "This order has already been paid.", OrderErrorCodes.StateConflict);
        return new VnPayUrlResponse(payment.Id, payment.PaymentNumber, payment.Amount,
            vnPayGateway.CreatePaymentUrl(payment, returnUri.AbsoluteUri, clientIpAddress ?? "127.0.0.1",
                clock.UtcNow));
    }

    public async Task<VietQrPaymentResponse> CreateBankTransferAsync(Guid customerId, CreateBankTransferRequest request, CancellationToken cancellationToken = default)
    {
        var isOrderPayment = request.OrderId.HasValue && !request.ContractId.HasValue && !request.MilestoneId.HasValue;
        var isMilestonePayment = !request.OrderId.HasValue && request.ContractId.HasValue && request.MilestoneId.HasValue;
        if (!isOrderPayment && !isMilestonePayment)
            throw new HttpException(400, "Provide either an order ID or both a contract ID and milestone ID.", OrderErrorCodes.ValidationError);

        decimal amount;
        if (isOrderPayment)
        {
            var bankTransferEnabled = await GetBoolConfigAsync(
                OrderPolicyDefaults.DirectOrderBankTransferEnabledKey,
                OrderPolicyDefaults.DefaultDirectOrderBankTransferEnabled,
                cancellationToken);
            if (!bankTransferEnabled)
                throw new HttpException(422, "Direct bank transfer is disabled for direct bulk orders. Please pay via VNPay.", OrderErrorCodes.PaymentMethodNotSupported);

            var order = await repository.GetOrderForPaymentAsync(customerId, request.OrderId!.Value, cancellationToken)
                        ?? throw new HttpException(404, B2BOrderingMessages.OrderNotFound);
            if (order.Status is OrderStatus.Cancelled or OrderStatus.Pending)
                throw new HttpException(409, "A payment cannot be created for an unconfirmed or cancelled order.", OrderErrorCodes.StateConflict);
            amount = order.TotalAmount;
        }
        else
        {
            var target = await repository.GetMilestoneForPaymentAsync(customerId, request.ContractId!.Value, request.MilestoneId!.Value, cancellationToken);
            if (target.Contract is null || target.Milestone is null)
                throw new HttpException(404, "Payable contract milestone was not found.");
            amount = target.Milestone.Amount ?? 0m;
        }
        if (amount <= 0)
            throw new HttpException(409, "The payable amount must be greater than zero.", OrderErrorCodes.StateConflict);

        var now = clock.UtcNow;
        var deadlineHours = await GetIntConfigAsync(PaymentDeadlineConfigKey, 48, cancellationToken);
        var paymentNumber = $"PAY-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            ContractId = request.ContractId,
            MilestoneId = request.MilestoneId,
            CustomerId = customerId,
            PaymentNumber = paymentNumber,
            Amount = amount,
            Currency = "VND",
            Method = PaymentMethod.BankTransfer,
            Provider = "VietQR",
            Status = PaymentStatus.Pending,
            ExpiresAt = now.AddHours(deadlineHours),
            CreatedBy = customerId
        };

        if (!await repository.CreatePaymentAsync(payment, cancellationToken))
            throw new HttpException(409, "A payment is already in progress for this order or milestone.", OrderErrorCodes.StateConflict);

        const string bankName = "Ngân hàng Quân Đội (MB Bank)";
        const string bankBin = "970422";
        const string accountNumber = "0988668899";
        const string accountName = "CONG TY CP NGUYET PHO B2B";
        var transferMemo = $"PAY {paymentNumber}";
        var qrCodeUrl = $"https://img.vietqr.io/image/MB-0988668899-compact2.png?amount={decimal.ToInt64(amount)}&addInfo={Uri.EscapeDataString(transferMemo)}&accountName={Uri.EscapeDataString(accountName)}";

        return new VietQrPaymentResponse(
            payment.Id,
            payment.PaymentNumber,
            payment.Amount,
            bankName,
            bankBin,
            accountNumber,
            accountName,
            transferMemo,
            qrCodeUrl,
            payment.ExpiresAt!.Value
        );
    }

    public async Task<VnPayReturnCallbackResponse> VerifyReturnCallbackAsync(
        IReadOnlyDictionary<string, string> queryParams, CancellationToken cancellationToken = default,
        bool confirmPaymentFromReturn = false)
    {
        var isValid = vnPayGateway.IsValidSignature(queryParams);
        queryParams.TryGetValue("vnp_TxnRef", out var paymentNumber);
        queryParams.TryGetValue("vnp_ResponseCode", out var responseCode);
        queryParams.TryGetValue("vnp_BankCode", out var bankCode);
        queryParams.TryGetValue("vnp_TransactionNo", out var transactionNo);
        queryParams.TryGetValue("vnp_Amount", out var amountText);

        if (string.IsNullOrWhiteSpace(paymentNumber))
            throw new HttpException(400, "Missing payment transaction reference.", OrderErrorCodes.ValidationError);

        var payment = await repository.GetPaymentByNumberAsync(paymentNumber, cancellationToken)
            ?? throw new HttpException(404, "Payment record was not found.");

        var amountMatches = long.TryParse(amountText, NumberStyles.None, CultureInfo.InvariantCulture,
                out var receivedAmount) &&
            receivedAmount == decimal.Round(payment.Amount * 100m, 0, MidpointRounding.AwayFromZero);
        var isSuccess = isValid && responseCode == "00" && amountMatches && payment.Method == PaymentMethod.VNPay;

        if (isValid && !isSuccess && payment.Status is PaymentStatus.Pending or PaymentStatus.Processing)
        {
            // A valid, signed cancellation/failure may close the pending attempt. Do not mutate it
            // when the amount or payment method does not match the stored transaction.
            if (responseCode != "00" && amountMatches && payment.Method == PaymentMethod.VNPay)
            {
                await repository.FailVnPayPaymentAsync(paymentNumber, $"VNPay returned response code {responseCode ?? "missing"}.", cancellationToken);
                await NotifyPaymentResultAsync(payment, false, cancellationToken);
            }
        }
        else if (isSuccess && confirmPaymentFromReturn && payment.Status != PaymentStatus.Completed)
        {
            // For local development the browser can reach the Return URL while VNPay cannot reach
            // a localhost IPN endpoint. The return payload is signed by VNPay; verify its amount
            // above, then use the same idempotent settlement path as IPN.
            var confirmed = await repository.ConfirmVnPayPaymentAsync(paymentNumber, cancellationToken);
            if (confirmed?.Status == PaymentStatus.Completed)
            {
                payment = confirmed;
                await NotifyPaymentResultAsync(confirmed, true, cancellationToken);
            }
        }
        payment = await repository.GetPaymentByNumberAsync(paymentNumber, cancellationToken) ?? payment;

        return new VnPayReturnCallbackResponse(
            payment.PaymentNumber,
            payment.OrderId,
            payment.ContractId,
            payment.MilestoneId,
            payment.Amount,
            isSuccess,
            !isValid ? "97" : !amountMatches || payment.Method != PaymentMethod.VNPay ? "04" : (responseCode ?? "00"),
            bankCode,
            transactionNo,
            payment.PaidAt,
            payment.Status
        );
    }

    public async Task<PaymentResponse> UploadEvidenceAsync(Guid paymentId, Guid customerId, UploadPaymentEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        var payment = await repository.AttachEvidenceAsync(paymentId, customerId, request.EvidenceUrl, request.Notes, cancellationToken)
            ?? throw new HttpException(404, "Payment record was not found or access denied.", OrderErrorCodes.TenantForbidden);
        return ToResponse(payment);
    }

    public async Task<EscrowSummaryResponse> GetEscrowSummaryAsync(CancellationToken cancellationToken = default) =>
        await repository.GetEscrowSummaryAsync(cancellationToken);

    public async Task<(string Code, string Message)> ProcessVnPayIpnAsync(
        IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        if (!vnPayGateway.IsValidSignature(parameters)) return ("97", "Invalid Checksum");
        if (!parameters.TryGetValue("vnp_TxnRef", out var paymentNumber) || string.IsNullOrWhiteSpace(paymentNumber))
            return ("01", "Order not found");
        var payment = await repository.GetPaymentByNumberAsync(paymentNumber, cancellationToken);
        if (payment is null || payment.Method != PaymentMethod.VNPay) return ("01", "Order not found");
        if (payment.Status == PaymentStatus.Completed) return ("02", "Order already confirmed");
        if (!parameters.TryGetValue("vnp_Amount", out var amountText) ||
            !long.TryParse(amountText, NumberStyles.None, CultureInfo.InvariantCulture, out var receivedAmount) ||
            receivedAmount != decimal.Round(payment.Amount * 100m, 0, MidpointRounding.AwayFromZero))
            return ("04", "Invalid Amount");
        if (!parameters.TryGetValue("vnp_ResponseCode", out var responseCode) || responseCode != "00")
        {
            if (payment.Status is PaymentStatus.Pending or PaymentStatus.Processing)
            {
                await repository.FailVnPayPaymentAsync(paymentNumber,
                    $"VNPay returned response code {responseCode ?? "missing"}.", cancellationToken);
                await NotifyPaymentResultAsync(payment, false, cancellationToken);
            }
            return ("00", "Confirm Success");
        }
        var confirmed = await repository.ConfirmVnPayPaymentAsync(paymentNumber, cancellationToken);
        if (payment.Status != PaymentStatus.Completed && confirmed?.Status == PaymentStatus.Completed)
            await NotifyPaymentResultAsync(confirmed, true, cancellationToken);
        return confirmed is null ? ("99", "Unknown error") : ("00", "Confirm Success");
    }

    private Task NotifyPaymentResultAsync(Payment payment, bool succeeded, CancellationToken ct) =>
        notificationService.DispatchNotificationAsync(
            payment.CustomerId,
            succeeded ? "payment.completed" : "payment.failed",
            succeeded ? "Thanh toán thành công" : "Thanh toán chưa thành công",
            succeeded
                ? $"Giao dịch {payment.PaymentNumber} đã được VNPay xác nhận thành công với số tiền {payment.Amount:N0} VND."
                : $"Giao dịch {payment.PaymentNumber} chưa thành công. Bạn có thể kiểm tra lịch sử và thử thanh toán lại.",
            "payment",
            payment.Id,
            payment.ContractId.HasValue ? "/#/hop-dong-tien-do" : "/#/don-hang-cua-toi",
            succeeded ? "high" : "medium",
            ct);

    public async Task<PaymentResponse> ConfirmAsync(Guid paymentId, Guid adminUserId, string reason, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var payment = await repository.ConfirmPaymentAsync(paymentId, adminUserId, reason.Trim(), ipAddress, cancellationToken)
                      ?? throw new HttpException(409, "Only a pending or processing payment can be confirmed.", OrderErrorCodes.StateConflict);
        return ToResponse(payment);
    }

    public async Task<PaymentResponse> RefundAsync(Guid paymentId, Guid adminUserId, string reason, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var payment = await repository.RefundPaymentAsync(paymentId, adminUserId, reason.Trim(), ipAddress, cancellationToken)
                      ?? throw new HttpException(409, "This payment cannot be refunded.", OrderErrorCodes.StateConflict);
        return ToResponse(payment);
    }

    private async Task<bool> GetBoolConfigAsync(string key, bool fallback, CancellationToken cancellationToken)
    {
        var value = await adminRepository.GetSystemConfigAsync(key, cancellationToken);
        if (value is null || string.IsNullOrWhiteSpace(value.ValueJson)) return fallback;
        try
        {
            var element = JsonDocument.Parse(value.ValueJson).RootElement;
            return element.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(element.GetString(), out var parsed) => parsed,
                _ => fallback
            };
        }
        catch (JsonException)
        {
            return bool.TryParse(value.ValueJson.Trim(), out var parsed) ? parsed : fallback;
        }
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
        payment.ContractId, payment.MilestoneId, payment.Amount, payment.Currency, payment.Method, payment.Status, payment.PaidAt, payment.ExpiresAt,
        payment.EvidenceUrl, payment.RefundedAmount, payment.RefundReason, payment.CreatedAt);
}
