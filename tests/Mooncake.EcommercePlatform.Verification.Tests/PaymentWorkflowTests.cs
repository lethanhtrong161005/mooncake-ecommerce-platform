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
}

