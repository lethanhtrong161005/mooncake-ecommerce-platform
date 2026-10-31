namespace Mooncake.EcommercePlatform.Verification.Tests;

using Moq;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Xunit;

public sealed class PaymentWorkflowTests
{
    private readonly Mock<IPaymentRepository> repository = new();
    private readonly Mock<IAdminPlatformRepository> adminRepository = new();
    private readonly Mock<IVnPayGateway> vnPayGateway = new();
    private readonly Mock<IDateTimeProvider> clock = new();
    private readonly Guid customerId = Guid.NewGuid();
    private readonly Guid contractId = Guid.NewGuid();
    private readonly Guid milestoneId = Guid.NewGuid();
    private readonly DateTime fixedNow = new(2026, 10, 29, 9, 0, 0, DateTimeKind.Utc);

    public PaymentWorkflowTests()
    {
        clock.SetupGet(c => c.UtcNow).Returns(fixedNow);
    }

    [Fact]
    public async Task CreateVnPayUrlAsync_ReturnsSignedUrl_WhenDepositReady()
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ContractId = contractId,
            MilestoneId = milestoneId,
            CustomerId = customerId,
            PaymentNumber = "PAY-20261029-DEP001",
            Amount = 15000000m,
            Currency = "VND",
            Method = PaymentMethod.VNPay,
            Status = PaymentStatus.Pending
        };

        repository.Setup(r => r.PrepareVnPayPaymentAsync(customerId, contractId, milestoneId, default))
            .ReturnsAsync(payment);
        vnPayGateway.Setup(g => g.CreatePaymentUrl(payment, "http://localhost:5173/#/hop-dong-tien-do", "127.0.0.1", fixedNow))
            .Returns("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=1500000000&vnp_SecureHash=abcdef");

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var response = await service.CreateVnPayUrlAsync(customerId, new CreateVnPayUrlRequest
        {
            ContractId = contractId,
            MilestoneId = milestoneId,
            ReturnUrl = "http://localhost:5173/#/hop-dong-tien-do"
        }, "127.0.0.1");

        Assert.Equal(payment.Id, response.PaymentId);
        Assert.Equal(payment.PaymentNumber, response.PaymentNumber);
        Assert.Equal(15000000m, response.Amount);
        Assert.Contains("vnp_Amount=1500000000", response.PaymentUrl);
    }

    [Fact]
    public async Task ProcessVnPayIpnAsync_ReturnsInvalidChecksumCode97_WhenSignatureInvalid()
    {
        var parameters = new Dictionary<string, string>
        {
            ["vnp_TxnRef"] = "PAY-20261029-DEP001",
            ["vnp_Amount"] = "1500000000",
            ["vnp_ResponseCode"] = "00",
            ["vnp_SecureHash"] = "tampered-hash"
        };

        vnPayGateway.Setup(g => g.IsValidSignature(parameters)).Returns(false);

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var (code, message) = await service.ProcessVnPayIpnAsync(parameters);

        Assert.Equal("97", code);
        Assert.Equal("Invalid Checksum", message);
        repository.Verify(r => r.ConfirmVnPayPaymentAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task ProcessVnPayIpnAsync_ConfirmsPaymentAndMilestone_WhenSignatureValidAndResponseCode00()
    {
        const string paymentNumber = "PAY-20261029-DEP001";
        var parameters = new Dictionary<string, string>
        {
            ["vnp_TxnRef"] = paymentNumber,
            ["vnp_Amount"] = "1500000000",
            ["vnp_ResponseCode"] = "00",
            ["vnp_SecureHash"] = "valid-hash"
        };
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentNumber = paymentNumber,
            Amount = 15000000m,
            Method = PaymentMethod.VNPay,
            Status = PaymentStatus.Pending
        };

        vnPayGateway.Setup(g => g.IsValidSignature(parameters)).Returns(true);
        repository.Setup(r => r.GetPaymentByNumberAsync(paymentNumber, default)).ReturnsAsync(payment);
        repository.Setup(r => r.ConfirmVnPayPaymentAsync(paymentNumber, default)).ReturnsAsync(payment);

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var (code, message) = await service.ProcessVnPayIpnAsync(parameters);

        Assert.Equal("00", code);
        Assert.Equal("Confirm Success", message);
        repository.Verify(r => r.ConfirmVnPayPaymentAsync(paymentNumber, default), Times.Once);
    }

    [Fact]
    public async Task ProcessVnPayIpnAsync_RecordsFailure_WhenVnPayReturnsErrorResponse()
    {
        const string paymentNumber = "PAY-20261029-DEP001";
        var parameters = new Dictionary<string, string>
        {
            ["vnp_TxnRef"] = paymentNumber,
            ["vnp_Amount"] = "1500000000",
            ["vnp_ResponseCode"] = "24", // Customer cancelled payment
            ["vnp_SecureHash"] = "valid-hash"
        };
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentNumber = paymentNumber,
            Amount = 15000000m,
            Method = PaymentMethod.VNPay,
            Status = PaymentStatus.Pending
        };

        vnPayGateway.Setup(g => g.IsValidSignature(parameters)).Returns(true);
        repository.Setup(r => r.GetPaymentByNumberAsync(paymentNumber, default)).ReturnsAsync(payment);

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var (code, message) = await service.ProcessVnPayIpnAsync(parameters);

        Assert.Equal("00", code);
        repository.Verify(r => r.FailVnPayPaymentAsync(paymentNumber, It.Is<string>(s => s.Contains("24")), default), Times.Once);
        repository.Verify(r => r.ConfirmVnPayPaymentAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CreatesPendingPaymentWithConfiguredDefaultDeadline_ForConfirmedOrder()
    {
        var orderId = Guid.NewGuid();
        var order = new Order { Id = orderId, CustomerId = customerId, TotalAmount = 275000m, Status = OrderStatus.Confirmed };
        Payment? persisted = null;
        repository.Setup(r => r.GetOrderForPaymentAsync(customerId, orderId, default)).ReturnsAsync(order);
        repository.Setup(r => r.CreatePaymentAsync(It.IsAny<Payment>(), default))
            .Callback<Payment, CancellationToken>((payment, _) => persisted = payment).ReturnsAsync(true);

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var response = await service.CreateAsync(customerId, "order-payment-001", new CreatePaymentRequest
        {
            OrderId = orderId, Method = PaymentMethod.BankTransfer
        });

        Assert.NotNull(persisted);
        Assert.Equal(response.Id, persisted.Id);
        Assert.Equal("order-payment-001", persisted.IdempotencyKey);
        Assert.Equal(275000m, persisted.Amount);
        Assert.Equal(PaymentStatus.Pending, persisted.Status);
        Assert.Equal(fixedNow.AddHours(48), persisted.ExpiresAt);
    }

    [Fact]
    public async Task CreateAsync_ReturnsExistingPayment_WhenIdempotencyKeyAndPayloadMatch()
    {
        var orderId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(), CustomerId = customerId, OrderId = orderId,
            IdempotencyKey = "payment-replay-01", PaymentNumber = "PAY-REPLAY-01",
            Amount = 125000m, Method = PaymentMethod.BankTransfer, Status = PaymentStatus.Pending
        };
        repository.Setup(r => r.GetPaymentByIdempotencyKeyAsync(customerId, "payment-replay-01", default))
            .ReturnsAsync(payment);

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var response = await service.CreateAsync(customerId, " payment-replay-01 ", new CreatePaymentRequest
        {
            OrderId = orderId, Method = PaymentMethod.BankTransfer
        });

        Assert.Equal(payment.Id, response.Id);
        Assert.Equal(payment.PaymentNumber, response.PaymentNumber);
        repository.Verify(r => r.GetOrderForPaymentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default), Times.Never);
        repository.Verify(r => r.CreatePaymentAsync(It.IsAny<Payment>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RejectsReusingIdempotencyKeyForDifferentPayload()
    {
        var orderId = Guid.NewGuid();
        repository.Setup(r => r.GetPaymentByIdempotencyKeyAsync(customerId, "payment-replay-02", default))
            .ReturnsAsync(new Payment { OrderId = orderId, Method = PaymentMethod.BankTransfer });

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var error = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(
            customerId, "payment-replay-02", new CreatePaymentRequest { OrderId = orderId, Method = PaymentMethod.VNPay }));

        Assert.Equal(409, error.StatusCode);
        repository.Verify(r => r.CreatePaymentAsync(It.IsAny<Payment>(), default), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("short")]
    public async Task CreateAsync_RejectsMissingOrTooShortIdempotencyKey(string? key)
    {
        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var error = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(
            customerId, key, new CreatePaymentRequest { OrderId = Guid.NewGuid(), Method = PaymentMethod.BankTransfer }));
        Assert.Equal(400, error.StatusCode);
        repository.Verify(r => r.GetPaymentByIdempotencyKeyAsync(It.IsAny<Guid>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RejectsUnconfirmedOrderWithoutCreatingPayment()
    {
        var orderId = Guid.NewGuid();
        repository.Setup(r => r.GetOrderForPaymentAsync(customerId, orderId, default))
            .ReturnsAsync(new Order { Id = orderId, CustomerId = customerId, TotalAmount = 100000m, Status = OrderStatus.Pending });

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var error = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(
            customerId, "pending-order-payment", new CreatePaymentRequest { OrderId = orderId, Method = PaymentMethod.BankTransfer }));

        Assert.Equal(409, error.StatusCode);
        repository.Verify(r => r.CreatePaymentAsync(It.IsAny<Payment>(), default), Times.Never);
    }

    [Theory]
    [InlineData("1499999999")]
    [InlineData("not-an-amount")]
    [InlineData(null)]
    public async Task ProcessVnPayIpnAsync_RejectsMissingMalformedOrMismatchedAmount(string? amount)
    {
        const string paymentNumber = "PAY-20261029-AMOUNT";
        var parameters = new Dictionary<string, string>
        {
            ["vnp_TxnRef"] = paymentNumber, ["vnp_ResponseCode"] = "00", ["vnp_SecureHash"] = "valid-hash"
        };
        if (amount is not null) parameters["vnp_Amount"] = amount;
        vnPayGateway.Setup(g => g.IsValidSignature(parameters)).Returns(true);
        repository.Setup(r => r.GetPaymentByNumberAsync(paymentNumber, default)).ReturnsAsync(new Payment
        {
            PaymentNumber = paymentNumber, Amount = 15000000m, Method = PaymentMethod.VNPay, Status = PaymentStatus.Pending
        });

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var result = await service.ProcessVnPayIpnAsync(parameters);

        Assert.Equal("04", result.Code);
        Assert.Equal("Invalid Amount", result.Message);
        repository.Verify(r => r.ConfirmVnPayPaymentAsync(It.IsAny<string>(), default), Times.Never);
        repository.Verify(r => r.FailVnPayPaymentAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task ProcessVnPayIpnAsync_DoesNotConfirmAlreadyCompletedPayment()
    {
        const string paymentNumber = "PAY-20261029-DUPLICATE";
        var parameters = new Dictionary<string, string>
        {
            ["vnp_TxnRef"] = paymentNumber, ["vnp_Amount"] = "1500000000",
            ["vnp_ResponseCode"] = "00", ["vnp_SecureHash"] = "valid-hash"
        };
        vnPayGateway.Setup(g => g.IsValidSignature(parameters)).Returns(true);
        repository.Setup(r => r.GetPaymentByNumberAsync(paymentNumber, default)).ReturnsAsync(new Payment
        {
            PaymentNumber = paymentNumber, Amount = 15000000m, Method = PaymentMethod.VNPay, Status = PaymentStatus.Completed
        });

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var result = await service.ProcessVnPayIpnAsync(parameters);

        Assert.Equal("02", result.Code);
        repository.Verify(r => r.ConfirmVnPayPaymentAsync(It.IsAny<string>(), default), Times.Never);
    }
}

