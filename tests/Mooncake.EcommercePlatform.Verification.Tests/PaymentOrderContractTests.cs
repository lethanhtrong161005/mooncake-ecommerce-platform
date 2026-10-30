namespace Mooncake.EcommercePlatform.Verification.Tests;

using Moq;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Xunit;

public sealed class PaymentOrderContractTests
{
    private readonly Mock<IPaymentRepository> repository = new();
    private readonly Mock<IAdminPlatformRepository> adminRepository = new();
    private readonly Mock<IVnPayGateway> vnPayGateway = new();
    private readonly Mock<IDateTimeProvider> clock = new();
    private readonly Guid customerId = Guid.NewGuid();
    private readonly Guid orderId = Guid.NewGuid();
    private readonly Guid contractId = Guid.NewGuid();
    private readonly Guid milestoneId = Guid.NewGuid();
    private readonly DateTime fixedNow = new(2026, 10, 29, 9, 30, 0, DateTimeKind.Utc);

    public PaymentOrderContractTests()
    {
        clock.SetupGet(c => c.UtcNow).Returns(fixedNow);
    }

    [Fact]
    public async Task CreateOrderVnPayUrlAsync_ReturnsSignedUrl_WhenOrderIsValid()
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            CustomerId = customerId,
            PaymentNumber = "PAY-ORD-20261029-001",
            Amount = 4500000m,
            Currency = "VND",
            Method = PaymentMethod.VNPay,
            Status = PaymentStatus.Pending
        };

        repository.Setup(r => r.PrepareOrderVnPayPaymentAsync(customerId, orderId, default))
            .ReturnsAsync(payment);
        vnPayGateway.Setup(g => g.CreatePaymentUrl(payment, "http://localhost:5173/#/thanh-toan/ket-qua", "127.0.0.1", fixedNow))
            .Returns("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=450000000&vnp_TxnRef=PAY-ORD-20261029-001");

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var request = new CreateOrderVnPayUrlRequest
        {
            OrderId = orderId,
            ReturnUrl = "http://localhost:5173/#/thanh-toan/ket-qua"
        };

        var response = await service.CreateOrderVnPayUrlAsync(customerId, request, "127.0.0.1");

        Assert.Equal(payment.Id, response.PaymentId);
        Assert.Equal(payment.PaymentNumber, response.PaymentNumber);
        Assert.Equal(4500000m, response.Amount);
        Assert.Contains("sandbox.vnpayment.vn", response.PaymentUrl);
    }

    [Fact]
    public async Task CreateBankTransferAsync_ForContractMilestone_GeneratesVietQrAndCorrectMemo()
    {
        var contract = new Contract
        {
            Id = contractId,
            CustomerId = customerId,
            Status = ContractStatus.PendingDeposit
        };
        var milestone = new ContractMilestone
        {
            Id = milestoneId,
            ContractId = contractId,
            Amount = 12000000m,
            Status = MilestoneStatus.AwaitingPayment
        };

        repository.Setup(r => r.GetMilestoneForPaymentAsync(customerId, contractId, milestoneId, default))
            .ReturnsAsync((contract, milestone));
        repository.Setup(r => r.CreatePaymentAsync(It.IsAny<Payment>(), default))
            .ReturnsAsync(true);

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var request = new CreateBankTransferRequest
        {
            ContractId = contractId,
            MilestoneId = milestoneId
        };

        var response = await service.CreateBankTransferAsync(customerId, request);

        Assert.NotEqual(Guid.Empty, response.PaymentId);
        Assert.StartsWith("PAY-", response.PaymentNumber);
        Assert.Equal(12000000m, response.Amount);
        Assert.Equal("Ngân hàng Quân Đội (MB Bank)", response.BankName);
        Assert.Equal("0988668899", response.AccountNumber);
        Assert.Equal("CONG TY CP NGUYET PHO B2B", response.AccountName);
        Assert.Equal($"PAY {response.PaymentNumber}", response.TransferMemo);
        Assert.Contains("img.vietqr.io/image/MB-0988668899-compact2.png", response.QrCodeUrl);
        repository.Verify(r => r.CreatePaymentAsync(It.Is<Payment>(p => p.Method == PaymentMethod.BankTransfer && p.Amount == 12000000m), default), Times.Once);
    }

    [Fact]
    public async Task CreateBankTransferAsync_ForOrder_GeneratesVietQrWithOrderMemo()
    {
        var order = new Order
        {
            Id = orderId,
            CustomerId = customerId,
            TotalAmount = 8500000m,
            Status = OrderStatus.Confirmed
        };

        repository.Setup(r => r.GetOrderForPaymentAsync(customerId, orderId, default))
            .ReturnsAsync(order);
        repository.Setup(r => r.CreatePaymentAsync(It.IsAny<Payment>(), default))
            .ReturnsAsync(true);

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var request = new CreateBankTransferRequest
        {
            OrderId = orderId
        };

        var response = await service.CreateBankTransferAsync(customerId, request);

        Assert.NotEqual(Guid.Empty, response.PaymentId);
        Assert.StartsWith("PAY-", response.PaymentNumber);
        Assert.Equal($"PAY {response.PaymentNumber}", response.TransferMemo);
        Assert.Equal(8500000m, response.Amount);
        repository.Verify(r => r.CreatePaymentAsync(It.Is<Payment>(p => p.Method == PaymentMethod.BankTransfer && p.OrderId == orderId), default), Times.Once);
    }

    [Fact]
    public async Task CreateBankTransferAsync_ThrowsHttpException400_WhenNeitherContractNorOrderProvided()
    {
        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var request = new CreateBankTransferRequest();

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.CreateBankTransferAsync(customerId, request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task VerifyReturnCallbackAsync_ReturnsSuccess_WhenSignatureValidAndResponseCode00()
    {
        const string paymentNumber = "PAY-20261029-999";
        var queryParams = new Dictionary<string, string>
        {
            ["vnp_TxnRef"] = paymentNumber,
            ["vnp_Amount"] = "1500000000",
            ["vnp_ResponseCode"] = "00",
            ["vnp_SecureHash"] = "correct-signature",
            ["vnp_BankCode"] = "NCB",
            ["vnp_TransactionNo"] = "14088899",
            ["vnp_PayDate"] = "20261029103000"
        };

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentNumber = paymentNumber,
            Amount = 15000000m,
            CustomerId = customerId,
            Method = PaymentMethod.VNPay,
            Status = PaymentStatus.Pending
        };

        repository.Setup(r => r.GetPaymentByNumberAsync(paymentNumber, default))
            .ReturnsAsync(payment);
        repository.Setup(r => r.ConfirmVnPayPaymentAsync(paymentNumber, default))
            .ReturnsAsync(payment);
        vnPayGateway.Setup(g => g.IsValidSignature(queryParams)).Returns(true);

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var response = await service.VerifyReturnCallbackAsync(queryParams);

        Assert.True(response.IsSuccess);
        Assert.Equal(paymentNumber, response.PaymentNumber);
        Assert.Equal(15000000m, response.Amount);
        Assert.Equal("00", response.ResponseCode);
        Assert.Equal("NCB", response.BankCode);
        Assert.Equal("14088899", response.TransactionNo);
        repository.Verify(r => r.ConfirmVnPayPaymentAsync(paymentNumber, default), Times.Once);
    }

    [Fact]
    public async Task VerifyReturnCallbackAsync_ReturnsFailure_WhenSignatureIsTampered()
    {
        const string paymentNumber = "PAY-20261029-999";
        var queryParams = new Dictionary<string, string>
        {
            ["vnp_TxnRef"] = paymentNumber,
            ["vnp_Amount"] = "1500000000",
            ["vnp_ResponseCode"] = "00",
            ["vnp_SecureHash"] = "tampered"
        };

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentNumber = paymentNumber,
            Amount = 15000000m,
            CustomerId = customerId,
            Method = PaymentMethod.VNPay,
            Status = PaymentStatus.Pending
        };

        repository.Setup(r => r.GetPaymentByNumberAsync(paymentNumber, default))
            .ReturnsAsync(payment);
        vnPayGateway.Setup(g => g.IsValidSignature(queryParams)).Returns(false);

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var response = await service.VerifyReturnCallbackAsync(queryParams);

        Assert.False(response.IsSuccess);
        Assert.Equal("97", response.ResponseCode);
        repository.Verify(r => r.ConfirmVnPayPaymentAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task UploadEvidenceAsync_UpdatesPaymentAndAttachesNotes()
    {
        var paymentId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = paymentId,
            CustomerId = customerId,
            PaymentNumber = "PAY-UNC-001",
            Amount = 5000000m,
            Method = PaymentMethod.BankTransfer,
            Status = PaymentStatus.Processing,
            EvidenceUrl = "https://storage.mooncake.vn/evidence/unc-123.jpg",
            EvidenceNotes = "Ủy nhiệm chi Vietcombank chuyển khoản đợt 1"
        };

        repository.Setup(r => r.AttachEvidenceAsync(paymentId, customerId, "https://storage.mooncake.vn/evidence/unc-123.jpg", "Ủy nhiệm chi Vietcombank chuyển khoản đợt 1", default))
            .ReturnsAsync(payment);

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var request = new UploadPaymentEvidenceRequest
        {
            EvidenceUrl = "https://storage.mooncake.vn/evidence/unc-123.jpg",
            Notes = "Ủy nhiệm chi Vietcombank chuyển khoản đợt 1"
        };

        var response = await service.UploadEvidenceAsync(paymentId, customerId, request);

        Assert.Equal(paymentId, response.Id);
        Assert.Equal("https://storage.mooncake.vn/evidence/unc-123.jpg", response.EvidenceUrl);
        Assert.Equal(PaymentStatus.Processing, response.Status);
    }

    [Fact]
    public async Task CreateOrderAsync_ValidatesCodMaxLimit_AndCustomPackaging()
    {
        // COD max 10,000,000 VND
        var highValueOrder = new Order
        {
            Id = orderId,
            CustomerId = customerId,
            TotalAmount = 15000000m,
            Status = OrderStatus.Confirmed
        };
        repository.Setup(r => r.GetOrderForPaymentAsync(customerId, orderId, default))
            .ReturnsAsync(highValueOrder);

        var service = new PaymentService(repository.Object, adminRepository.Object, vnPayGateway.Object, clock.Object);
        var request = new CreatePaymentRequest
        {
            OrderId = orderId,
            Method = PaymentMethod.Cash
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(customerId, "idempotency-key-order-cod-01", request));
        Assert.Equal(422, ex.StatusCode);
        Assert.Contains("Cash on delivery is not enabled for this amount", ex.Message);

        // Custom packaging order with COD
        var customPackagingOrder = new Order
        {
            Id = orderId,
            CustomerId = customerId,
            TotalAmount = 5000000m,
            PackagingFee = 500000m,
            Status = OrderStatus.Confirmed
        };
        repository.Setup(r => r.GetOrderForPaymentAsync(customerId, orderId, default))
            .ReturnsAsync(customPackagingOrder);

        var customPkgEx = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(customerId, "idempotency-key-order-cod-02", request));
        Assert.Equal(422, customPkgEx.StatusCode);
        Assert.Contains("Cash on delivery is not available for orders with custom packaging", customPkgEx.Message);
    }
}
