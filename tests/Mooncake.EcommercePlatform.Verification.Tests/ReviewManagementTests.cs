namespace Mooncake.EcommercePlatform.Verification.Tests;

using Moq;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Xunit;

public sealed class ReviewManagementTests
{
    private readonly Mock<IReviewRepository> repository = new();
    private readonly Mock<IDateTimeProvider> clock = new();

    private readonly Guid customerUserId = Guid.NewGuid();
    private readonly Guid customerProfileId = Guid.NewGuid();
    private readonly Guid supplierUserId = Guid.NewGuid();
    private readonly Guid supplierId = Guid.NewGuid();
    private readonly Guid orderId = Guid.NewGuid();
    private readonly Guid contractId = Guid.NewGuid();
    private readonly Guid productId = Guid.NewGuid();
    private readonly Guid orderItemId = Guid.NewGuid();
    private readonly Guid reviewId = Guid.NewGuid();
    private readonly DateTime fixedNow = new(2026, 10, 29, 10, 0, 0, DateTimeKind.Utc);

    public ReviewManagementTests()
    {
        clock.SetupGet(c => c.UtcNow).Returns(fixedNow);

        repository.Setup(r => r.GetCustomerProfileByUserIdAsync(customerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerProfile
            {
                Id = customerProfileId,
                UserId = customerUserId,
                CompanyName = "Tập đoàn VinFast",
                CompanyLogoUrl = "https://vinfast.vn/logo.png"
            });

        repository.Setup(r => r.GetSupplierProfileAsync(supplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupplierProfile
            {
                Id = supplierId,
                UserId = supplierUserId,
                CompanyName = "Xưởng Bánh Tràng An Nghệ Nhân",
                ReputationScore = 50.0m
            });

        repository.Setup(r => r.GetSupplierProfileByUserIdAsync(supplierUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupplierProfile
            {
                Id = supplierId,
                UserId = supplierUserId,
                CompanyName = "Xưởng Bánh Tràng An Nghệ Nhân",
                ReputationScore = 50.0m
            });
    }

    [Fact]
    public async Task CreateTransactionReview_With5Stars_IncrementsReputationBy3Points()
    {
        var service = new ReviewService(repository.Object, clock.Object);
        var request = new CreateTransactionReviewRequest
        {
            OrderId = orderId,
            QualityRating = 5,
            DeliveryRating = 5,
            ServiceRating = 5,
            OverallRating = 5,
            Comment = "Bánh rất ngon và giao đúng hẹn!",
            IsAnonymous = false
        };

        var order = new Order
        {
            Id = orderId,
            CustomerId = customerProfileId,
            OrderNumber = "ORD-202610-001",
            Status = OrderStatus.Delivered
        };

        repository.Setup(r => r.GetOrderWithSupplierForCustomerAsync(customerUserId, orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((order, supplierId));

        repository.Setup(r => r.GetReviewByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Review?)null);

        repository.Setup(r => r.CreateReviewAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Review r, CancellationToken _) => r);

        repository.Setup(r => r.UpdateSupplierReputationScoreAsync(supplierId, 3.0m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(53.0m);

        var result = await service.CreateTransactionReviewAsync(customerUserId, request);

        Assert.NotNull(result);
        Assert.Equal(5, result.OverallRating);
        Assert.Equal(3.0m, result.ReputationScoreDelta);
        Assert.Equal(53.0m, result.NewSupplierReputationScore);
        Assert.Equal("ORD-202610-001", result.OrderNumber);
        repository.Verify(r => r.AddReputationLogAsync(It.Is<ReputationLog>(l => l.EventType == ReputationEvent.PositiveReview && l.ScoreDelta == 3.0m), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionReview_With1Or2Stars_DecrementsReputationBy2Points()
    {
        var service = new ReviewService(repository.Object, clock.Object);
        var request = new CreateTransactionReviewRequest
        {
            OrderId = orderId,
            QualityRating = 1,
            DeliveryRating = 2,
            ServiceRating = 1,
            OverallRating = 2,
            Comment = "Bánh bị móp méo khi vận chuyển.",
            IsAnonymous = true
        };

        var order = new Order
        {
            Id = orderId,
            CustomerId = customerProfileId,
            OrderNumber = "ORD-202610-002",
            Status = OrderStatus.Delivered
        };

        repository.Setup(r => r.GetOrderWithSupplierForCustomerAsync(customerUserId, orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((order, supplierId));

        repository.Setup(r => r.GetReviewByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Review?)null);

        repository.Setup(r => r.CreateReviewAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Review r, CancellationToken _) => r);

        repository.Setup(r => r.UpdateSupplierReputationScoreAsync(supplierId, -2.0m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(48.0m);

        var result = await service.CreateTransactionReviewAsync(customerUserId, request);

        Assert.NotNull(result);
        Assert.Equal(2, result.OverallRating);
        Assert.Equal(-2.0m, result.ReputationScoreDelta);
        Assert.Equal(48.0m, result.NewSupplierReputationScore);
        Assert.True(result.IsAnonymous);
        Assert.Equal("Khách hàng ẩn danh", result.CustomerDisplayName);
        repository.Verify(r => r.AddReputationLogAsync(It.Is<ReputationLog>(l => l.EventType == ReputationEvent.NegativeReview && l.ScoreDelta == -2.0m), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionReview_WhenNotDelivered_ThrowsBadRequestException()
    {
        var service = new ReviewService(repository.Object, clock.Object);
        var request = new CreateTransactionReviewRequest
        {
            OrderId = orderId,
            OverallRating = 5,
            Comment = "Bánh tốt"
        };

        var order = new Order
        {
            Id = orderId,
            CustomerId = customerProfileId,
            Status = OrderStatus.Processing // Not delivered yet!
        };

        repository.Setup(r => r.GetOrderWithSupplierForCustomerAsync(customerUserId, orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((order, supplierId));

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.CreateTransactionReviewAsync(customerUserId, request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateTransactionReview_WhenAlreadyReviewed_ThrowsConflictException()
    {
        var service = new ReviewService(repository.Object, clock.Object);
        var request = new CreateTransactionReviewRequest
        {
            OrderId = orderId,
            OverallRating = 5,
            Comment = "Đã đánh giá rồi"
        };

        var order = new Order
        {
            Id = orderId,
            CustomerId = customerProfileId,
            Status = OrderStatus.Delivered
        };

        repository.Setup(r => r.GetOrderWithSupplierForCustomerAsync(customerUserId, orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((order, supplierId));

        repository.Setup(r => r.GetReviewByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Review { Id = Guid.NewGuid(), OrderId = orderId }); // Already reviewed!

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.CreateTransactionReviewAsync(customerUserId, request));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task SupplierReply_WhenAlreadyReplied_ThrowsBadRequestException()
    {
        var service = new ReviewService(repository.Object, clock.Object);
        var request = new SupplierReplyRequest { ReplyContent = "Cảm ơn quý khách lần 2" };

        var review = new Review
        {
            Id = reviewId,
            SupplierId = supplierId,
            SupplierReply = "Đã cảm ơn lần 1 rồi!" // Already replied!
        };

        repository.Setup(r => r.GetReviewByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.ReplyTransactionReviewAsync(supplierUserId, reviewId, request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateProductReview_WhenNotPurchased_ThrowsForbiddenException()
    {
        var service = new ReviewService(repository.Object, clock.Object);
        var request = new CreateProductReviewRequest
        {
            OrderItemId = orderItemId,
            Rating = 5,
            Content = "Chưa mua nhưng muốn đánh giá ảo"
        };

        repository.Setup(r => r.GetProductReviewByOrderItemAsync(customerProfileId, orderItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

        // Not in eligible list!
        repository.Setup(r => r.GetEligibleOrderItemsForProductAsync(customerUserId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EligibleOrderItemDto>());

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.CreateProductReviewAsync(customerUserId, productId, request));
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task ProductReview_ToggleLike_IncrementsAndDecrementsCount()
    {
        var service = new ReviewService(repository.Object, clock.Object);
        var review = new ProductReview
        {
            Id = reviewId,
            LikeCount = 10
        };

        repository.Setup(r => r.GetProductReviewByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        // Step 1: Not liked -> likes review
        repository.Setup(r => r.GetReviewLikeAsync(reviewId, customerProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReviewLike?)null);

        var likeResult = await service.ToggleProductReviewLikeAsync(customerUserId, reviewId);
        Assert.True(likeResult.IsLiked);
        Assert.Equal(11, likeResult.LikeCount);

        // Step 2: Already liked -> unlikes review
        repository.Setup(r => r.GetReviewLikeAsync(reviewId, customerProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductReviewLike { ProductReviewId = reviewId, CustomerId = customerProfileId });

        var unlikeResult = await service.ToggleProductReviewLikeAsync(customerUserId, reviewId);
        Assert.False(unlikeResult.IsLiked);
        Assert.Equal(10, unlikeResult.LikeCount);
    }

    [Fact]
    public async Task AdminAdjustReputation_UpdatesScoreAndWritesAuditLog()
    {
        var service = new ReviewService(repository.Object, clock.Object);
        var adminUserId = Guid.NewGuid();
        var request = new AdminAdjustReputationRequest
        {
            EventType = ReputationEvent.AdminPenalty,
            ScoreDelta = -10.0m,
            Reason = "Vi phạm quy chuẩn đóng gói theo kết luận thanh tra #TT-88",
            ReferenceType = "AuditReport"
        };

        repository.Setup(r => r.UpdateSupplierReputationScoreAsync(supplierId, -10.0m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(40.0m);

        repository.Setup(r => r.GetReputationLogsAsync(supplierId, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReputationLog>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    SupplierId = supplierId,
                    EventType = ReputationEvent.AdminPenalty,
                    ScoreDelta = -10.0m,
                    Reason = request.Reason,
                    CreatedAt = fixedNow
                }
            });

        // When updated, profile has 40.0m
        repository.Setup(r => r.GetSupplierProfileAsync(supplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupplierProfile
            {
                Id = supplierId,
                CompanyName = "Xưởng Bánh Tràng An Nghệ Nhân",
                ReputationScore = 40.0m
            });

        var result = await service.AdminAdjustReputationAsync(adminUserId, supplierId, request);

        Assert.NotNull(result);
        Assert.Equal(40.0m, result.ReputationScore);
        Assert.Equal("Reliable", result.BadgeTier);
        Assert.Equal("Tin cậy", result.BadgeTitle);
        repository.Verify(r => r.AddReputationLogAsync(It.Is<ReputationLog>(l => l.ScoreDelta == -10.0m && l.EventType == ReputationEvent.AdminPenalty), It.IsAny<CancellationToken>()), Times.Once);
    }
}

