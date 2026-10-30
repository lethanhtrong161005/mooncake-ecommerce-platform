namespace Mooncake.EcommercePlatform.Verification.Tests;

using Moq;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Xunit;

public sealed class DeliveryManagementTests
{
    private readonly Mock<IDeliveryRepository> repository = new();
    private readonly Mock<IDateTimeProvider> clock = new();

    private readonly Guid userId = Guid.NewGuid();
    private readonly Guid contractId = Guid.NewGuid();
    private readonly Guid deliveryId = Guid.NewGuid();
    private readonly DateTime fixedNow = new(2026, 10, 29, 9, 0, 0, DateTimeKind.Utc);

    public DeliveryManagementTests()
    {
        clock.SetupGet(c => c.UtcNow).Returns(fixedNow);
    }

    [Fact]
    public async Task CreateAsync_WhenValidRequest_CreatesDeliverySuccessfully()
    {
        var service = new DeliveryService(repository.Object, clock.Object);
        var request = new CreateDeliveryRequest
        {
            ContractId = contractId,
            FromAddress = "Kho Xưởng Bát Tràng, Hà Nội",
            ToAddress = "Tòa Keangnam, Mễ Trì, Hà Nội",
            RecipientName = "Nguyễn Văn Hùng",
            RecipientPhone = "0912345678",
            ScheduledDate = DateOnly.FromDateTime(fixedNow.AddDays(2)),
            DeliveryType = DeliveryType.SelfArranged
        };

        repository.Setup(r => r.CreateAsync(It.IsAny<Delivery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Delivery d, CancellationToken _) => d);

        var result = await service.CreateAsync(userId, request);

        Assert.NotNull(result);
        Assert.Equal(DeliveryStatus.Pending, result.Status);
        Assert.Equal(request.FromAddress, result.FromAddress);
        Assert.Equal(request.ToAddress, result.ToAddress);
        Assert.Equal(request.RecipientName, result.RecipientName);
        Assert.StartsWith("DEL-", result.DeliveryNumber);
    }

    [Fact]
    public async Task CreateAsync_WhenBothContractAndOrderNull_ThrowsValidationException()
    {
        var service = new DeliveryService(repository.Object, clock.Object);
        var request = new CreateDeliveryRequest
        {
            ContractId = null,
            OrderId = null,
            FromAddress = "Kho",
            ToAddress = "Kho",
            RecipientName = "A",
            RecipientPhone = "0912345678"
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(userId, request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenInvalidPhone_ThrowsValidationException()
    {
        var service = new DeliveryService(repository.Object, clock.Object);
        var request = new CreateDeliveryRequest
        {
            ContractId = contractId,
            FromAddress = "Kho",
            ToAddress = "Kho",
            RecipientName = "A",
            RecipientPhone = "12345"
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(userId, request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenValidTransition_UpdatesStatus()
    {
        var service = new DeliveryService(repository.Object, clock.Object);
        var delivery = new Delivery
        {
            Id = deliveryId,
            ContractId = contractId,
            Status = DeliveryStatus.Pending,
            DeliveryNumber = "DEL-202610-001"
        };

        repository.Setup(r => r.GetByIdAsync(deliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);
        repository.Setup(r => r.CanManageDeliveryAsync(deliveryId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(r => r.GetProofsAsync(deliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeliveryProof>());
        repository.Setup(r => r.UpdateAsync(It.IsAny<Delivery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Delivery d, CancellationToken _) => d);

        var result = await service.UpdateStatusAsync(userId, deliveryId, new UpdateDeliveryStatusRequest
        {
            Status = DeliveryStatus.Confirmed,
            Notes = "Đã xác nhận chuyến xe."
        });

        Assert.Equal(DeliveryStatus.Confirmed, result.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenInvalidTransition_ThrowsValidationException()
    {
        var service = new DeliveryService(repository.Object, clock.Object);
        var delivery = new Delivery
        {
            Id = deliveryId,
            ContractId = contractId,
            Status = DeliveryStatus.Pending,
            DeliveryNumber = "DEL-202610-001"
        };

        repository.Setup(r => r.GetByIdAsync(deliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);
        repository.Setup(r => r.CanManageDeliveryAsync(deliveryId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.UpdateStatusAsync(userId, deliveryId, new UpdateDeliveryStatusRequest
        {
            Status = DeliveryStatus.Delivered
        }));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateStatusAsync_ToDeliveredWithoutProof_ThrowsValidationException()
    {
        var service = new DeliveryService(repository.Object, clock.Object);
        var delivery = new Delivery
        {
            Id = deliveryId,
            ContractId = contractId,
            Status = DeliveryStatus.InTransit,
            DeliveryNumber = "DEL-202610-001"
        };

        repository.Setup(r => r.GetByIdAsync(deliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);
        repository.Setup(r => r.CanManageDeliveryAsync(deliveryId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(r => r.GetProofsAsync(deliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeliveryProof>());

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.UpdateStatusAsync(userId, deliveryId, new UpdateDeliveryStatusRequest
        {
            Status = DeliveryStatus.Delivered
        }));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task SubmitProofAsync_DeliveryPoD_WithPhotoAndSignature_UpdatesStatusToDelivered()
    {
        var service = new DeliveryService(repository.Object, clock.Object);
        var delivery = new Delivery
        {
            Id = deliveryId,
            ContractId = contractId,
            Status = DeliveryStatus.InTransit,
            DeliveryNumber = "DEL-202610-001"
        };

        repository.Setup(r => r.GetByIdAsync(deliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);
        repository.Setup(r => r.CanManageDeliveryAsync(deliveryId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(r => r.CreateProofAsync(It.IsAny<DeliveryProof>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeliveryProof p, CancellationToken _) => p);
        repository.Setup(r => r.UpdateAsync(It.IsAny<Delivery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Delivery d, CancellationToken _) => d);

        var request = new CreateDeliveryProofRequest
        {
            ProofType = ProofType.Delivery,
            PhotoUrl = "https://storage.mooncake.vn/pod/photo1.jpg",
            SignatureUrl = "https://storage.mooncake.vn/pod/signature1.png",
            LocationLat = 21.0168m,
            LocationLng = 105.7838m,
            TakenBy = "Nguyễn Văn Hùng (Thủ kho)",
            Notes = "Đã giao đủ 1.500 hộp."
        };

        var result = await service.SubmitProofAsync(userId, deliveryId, request);

        Assert.NotNull(result);
        Assert.Equal(ProofType.Delivery, result.ProofType);
        Assert.Equal(request.PhotoUrl, result.PhotoUrl);
        Assert.Equal(request.SignatureUrl, result.SignatureUrl);
        Assert.NotNull(result.TamperProofHash);
        Assert.Equal(64, result.TamperProofHash.Length);
        Assert.True(result.IsVerified);
        Assert.Equal(DeliveryStatus.Delivered, delivery.Status);
    }

    [Fact]
    public async Task SubmitProofAsync_DeliveryPoD_MissingSignature_ThrowsValidationException()
    {
        var service = new DeliveryService(repository.Object, clock.Object);
        var delivery = new Delivery
        {
            Id = deliveryId,
            ContractId = contractId,
            Status = DeliveryStatus.InTransit,
            DeliveryNumber = "DEL-202610-001"
        };

        repository.Setup(r => r.GetByIdAsync(deliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);
        repository.Setup(r => r.CanManageDeliveryAsync(deliveryId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateDeliveryProofRequest
        {
            ProofType = ProofType.Delivery,
            PhotoUrl = "https://storage.mooncake.vn/pod/photo1.jpg",
            SignatureUrl = null // Missing signature
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.SubmitProofAsync(userId, deliveryId, request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task SubmitProofAsync_PickupPoD_UpdatesStatusToPickedUp()
    {
        var service = new DeliveryService(repository.Object, clock.Object);
        var delivery = new Delivery
        {
            Id = deliveryId,
            ContractId = contractId,
            Status = DeliveryStatus.Confirmed,
            DeliveryNumber = "DEL-202610-001"
        };

        repository.Setup(r => r.GetByIdAsync(deliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);
        repository.Setup(r => r.CanManageDeliveryAsync(deliveryId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(r => r.CreateProofAsync(It.IsAny<DeliveryProof>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeliveryProof p, CancellationToken _) => p);
        repository.Setup(r => r.UpdateAsync(It.IsAny<Delivery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Delivery d, CancellationToken _) => d);

        var request = new CreateDeliveryProofRequest
        {
            ProofType = ProofType.Pickup,
            PhotoUrl = "https://storage.mooncake.vn/pod/pickup.jpg",
            TakenBy = "Tài xế xưởng"
        };

        var result = await service.SubmitProofAsync(userId, deliveryId, request);

        Assert.NotNull(result);
        Assert.Equal(ProofType.Pickup, result.ProofType);
        Assert.Equal(DeliveryStatus.PickedUp, delivery.Status);
    }

    [Fact]
    public async Task VerifyProofIntegrityAsync_ValidProof_ReturnsTrue()
    {
        var service = new DeliveryService(repository.Object, clock.Object);
        var proofId = Guid.NewGuid();
        var proof = new DeliveryProof
        {
            Id = proofId,
            DeliveryId = deliveryId,
            TamperProofHash = new string('a', 64)
        };

        repository.Setup(r => r.GetProofByIdAsync(proofId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proof);

        var result = await service.VerifyProofIntegrityAsync(proofId);
        Assert.True(result);
    }
}

