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
            FromAddress = "Bat Trang Warehouse, Hanoi",
            ToAddress = "Keangnam Tower, Hanoi",
            RecipientName = "Nguyen Van Hung",
            RecipientPhone = "0912345678",
            ScheduledDate = DateOnly.FromDateTime(fixedNow.AddDays(2)),
            DeliveryType = DeliveryType.SelfArranged
        };

        repository.Setup(r => r.CanManageContractAsync(contractId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
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
            FromAddress = "Warehouse",
            ToAddress = "Destination",
            RecipientName = "Recipient",
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
            FromAddress = "Warehouse",
            ToAddress = "Destination",
            RecipientName = "Recipient",
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
            Notes = "Confirmed dispatch."
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
    public async Task SubmitProofAsync_DeliveryPoD_WithPhotoAndSignature_UpdatesContractDeliveryToDelivered()
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
            TakenBy = "Nguyen Van Hung",
            Notes = "Delivered 1,500 boxes."
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
    public async Task SubmitProofAsync_ContractDeliveryPoD_MissingSignature_ThrowsValidationException()
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
            SignatureUrl = null
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.SubmitProofAsync(userId, deliveryId, request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task SubmitProofAsync_OrderDeliveryPoD_WithoutSignature_DoesNotAutoMarkDelivered()
    {
        var service = new DeliveryService(repository.Object, clock.Object);
        var orderId = Guid.NewGuid();
        var delivery = new Delivery
        {
            Id = deliveryId,
            OrderId = orderId,
            Status = DeliveryStatus.InTransit,
            DeliveryNumber = "DEL-202610-ORD01"
        };

        repository.Setup(r => r.GetByIdAsync(deliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);
        repository.Setup(r => r.CanManageDeliveryAsync(deliveryId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(r => r.CreateProofAsync(It.IsAny<DeliveryProof>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeliveryProof p, CancellationToken _) => p);
        repository.Setup(r => r.UpdateAsync(It.IsAny<Delivery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Delivery d, CancellationToken _) => d);

        var result = await service.SubmitProofAsync(userId, deliveryId, new CreateDeliveryProofRequest
        {
            ProofType = ProofType.Delivery,
            PhotoUrl = "https://storage.mooncake.vn/pod/order-photo.jpg"
        });

        Assert.NotNull(result);
        Assert.Equal(DeliveryStatus.AwaitingCustomerConfirmation, delivery.Status);
        Assert.Null(delivery.ActualDeliveryDate);
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
            TakenBy = "Factory Driver"
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

    [Fact]
    public async Task CreateOrReshareLinkAsync_CreatesAndRotatesTokenWithExpiry()
    {
        var service = new DeliveryService(repository.Object, clock.Object);
        var delivery = new Delivery
        {
            Id = deliveryId,
            OrderId = Guid.NewGuid(),
            Status = DeliveryStatus.InTransit,
            DeliveryNumber = "DEL-202611-009"
        };

        repository.Setup(r => r.GetByIdAsync(deliveryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);
        repository.Setup(r => r.CanManageDeliveryAsync(deliveryId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(r => r.UpdateAsync(It.IsAny<Delivery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Delivery d, CancellationToken _) => d);

        var created = await service.CreateOrReshareLinkAsync(userId, deliveryId, new CreateDeliveryShareLinkRequest
        {
            ExpiresInHours = 12,
            Reshare = false
        });

        Assert.True(created.IsActive);
        Assert.False(string.IsNullOrWhiteSpace(created.Token));
        Assert.Equal(fixedNow.AddHours(12), created.ExpiresAtUtc);

        var reused = await service.CreateOrReshareLinkAsync(userId, deliveryId, new CreateDeliveryShareLinkRequest
        {
            ExpiresInHours = 24,
            Reshare = false
        });
        Assert.Equal(created.Token, reused.Token);

        var reshared = await service.CreateOrReshareLinkAsync(userId, deliveryId, new CreateDeliveryShareLinkRequest
        {
            ExpiresInHours = 48,
            Reshare = true
        });
        Assert.True(reshared.IsActive);
        Assert.NotEqual(created.Token, reshared.Token);
        Assert.Equal(fixedNow.AddHours(48), reshared.ExpiresAtUtc);
    }

    [Fact]
    public async Task GetPublicByTokenAsync_WhenExpiredOrRevoked_Throws410()
    {
        var service = new DeliveryService(repository.Object, clock.Object);
        var expiredDelivery = new Delivery
        {
            Id = deliveryId,
            OrderId = Guid.NewGuid(),
            Status = DeliveryStatus.InTransit,
            DeliveryNumber = "DEL-202611-010",
            ShareToken = "expired_token_value",
            ShareTokenExpiresAtUtc = fixedNow.AddMinutes(-1)
        };

        repository.Setup(r => r.GetByShareTokenAsync("expired_token_value", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredDelivery);

        var expiredEx = await Assert.ThrowsAsync<HttpException>(() =>
            service.GetPublicByTokenAsync("expired_token_value"));
        Assert.Equal(410, expiredEx.StatusCode);

        expiredDelivery.ShareTokenExpiresAtUtc = fixedNow.AddHours(5);
        expiredDelivery.ShareTokenRevokedAtUtc = fixedNow.AddMinutes(-1);

        var revokedEx = await Assert.ThrowsAsync<HttpException>(() =>
            service.GetPublicByTokenAsync("expired_token_value"));
        Assert.Equal(410, revokedEx.StatusCode);
    }

    [Fact]
    public async Task SubmitPublicProofAsync_MultiPhotoForOrder_CreatesAllProofsAndMovesToAwaitingCustomerConfirmation()
    {
        var service = new DeliveryService(repository.Object, clock.Object);
        var orderId = Guid.NewGuid();
        var delivery = new Delivery
        {
            Id = deliveryId,
            OrderId = orderId,
            Status = DeliveryStatus.InTransit,
            DeliveryNumber = "DEL-202611-011",
            ShareToken = "active_driver_token",
            ShareTokenExpiresAtUtc = fixedNow.AddHours(24),
            CreatedBy = userId
        };

        var createdProofs = new List<DeliveryProof>();
        repository.Setup(r => r.GetByShareTokenAsync("active_driver_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);
        repository.Setup(r => r.CreateProofAsync(It.IsAny<DeliveryProof>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeliveryProof p, CancellationToken _) =>
            {
                createdProofs.Add(p);
                return p;
            });
        repository.Setup(r => r.UpdateAsync(It.IsAny<Delivery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Delivery d, CancellationToken _) => d);

        var result = await service.SubmitPublicProofAsync("active_driver_token", new CreateDeliveryProofRequest
        {
            ProofType = ProofType.Delivery,
            PhotoUrls =
            [
                "https://storage.mooncake.vn/pod/1.jpg",
                "https://storage.mooncake.vn/pod/2.jpg"
            ],
            SignatureBase64 = "data:image/png;base64,AAA",
            TakenBy = "Nguyễn Văn Tài"
        });

        Assert.NotNull(result);
        Assert.Equal(2, createdProofs.Count);
        Assert.Equal(DeliveryStatus.AwaitingCustomerConfirmation, delivery.Status);
    }
}
