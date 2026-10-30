namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Text.Json;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Utils;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implementation of <see cref="IReviewService"/>.</summary>
public sealed class ReviewService : IReviewService
{
    private readonly IReviewRepository repository;
    private readonly IDateTimeProvider clock;

    public ReviewService(IReviewRepository repository, IDateTimeProvider clock)
    {
        this.repository = repository;
        this.clock = clock;
    }

    public async Task<TransactionReviewResponse> CreateTransactionReviewAsync(
        Guid customerUserId,
        CreateTransactionReviewRequest request,
        CancellationToken ct = default)
    {
        if (!request.OrderId.HasValue && !request.ContractId.HasValue)
        {
            throw new HttpException(400, "Bắt buộc phải cung cấp mã đơn hàng (OrderId) hoặc mã hợp đồng (ContractId).");
        }

        var customer = await repository.GetCustomerProfileByUserIdAsync(customerUserId, ct);
        if (customer == null)
        {
            throw new HttpException(403, "Không tìm thấy thông tin hồ sơ khách hàng của bạn.");
        }

        Guid supplierId;
        string supplierName = "Nhà cung cấp";
        string? orderNumber = null;
        string? contractNumber = null;
        Guid referenceId;
        string referenceType;
        string referenceCode;

        if (request.OrderId.HasValue)
        {
            var (order, orderSupplierId) = await repository.GetOrderWithSupplierForCustomerAsync(customerUserId, request.OrderId.Value, ct);
            if (order == null || !orderSupplierId.HasValue)
            {
                throw new HttpException(404, "Đơn hàng không tồn tại hoặc không thuộc quyền sở hữu của quý khách.");
            }

            if (order.Status != OrderStatus.Delivered)
            {
                throw new HttpException(400, "Đơn hàng chưa chuyển sang trạng thái hoàn thành giao nhận.");
            }

            var existing = await repository.GetReviewByOrderIdAsync(request.OrderId.Value, ct);
            if (existing != null)
            {
                throw new HttpException(409, "Giao dịch này đã được quý khách gửi đánh giá trước đó.");
            }

            supplierId = orderSupplierId.Value;
            orderNumber = order.OrderNumber;
            referenceId = order.Id;
            referenceType = "Order";
            referenceCode = order.OrderNumber;
        }
        else
        {
            var contract = await repository.GetContractForCustomerAsync(customerUserId, request.ContractId!.Value, ct);
            if (contract == null)
            {
                throw new HttpException(404, "Hợp đồng không tồn tại hoặc không thuộc quyền sở hữu của quý khách.");
            }

            if (contract.Status != ContractStatus.CompletedOnTime && contract.Status != ContractStatus.CompletedLate)
            {
                throw new HttpException(400, "Hợp đồng chưa hoàn thành nghiệm thu.");
            }

            var existing = await repository.GetReviewByContractIdAsync(request.ContractId.Value, ct);
            if (existing != null)
            {
                throw new HttpException(409, "Hợp đồng này đã được quý khách gửi đánh giá trước đó.");
            }

            supplierId = contract.SupplierId;
            contractNumber = contract.ContractNumber;
            referenceId = contract.Id;
            referenceType = "Contract";
            referenceCode = contract.ContractNumber;
        }

        var supplierProfile = await repository.GetSupplierProfileAsync(supplierId, ct);
        if (supplierProfile != null && !string.IsNullOrWhiteSpace(supplierProfile.CompanyName))
        {
            supplierName = supplierProfile.CompanyName;
        }

        decimal scoreDelta = 0m;
        ReputationEvent? eventType = null;
        string reason = string.Empty;

        if (request.OverallRating == 5)
        {
            scoreDelta = 3.0m;
            eventType = ReputationEvent.PositiveReview;
            reason = $"Khách hàng đánh giá 5 sao xuất sắc cho giao dịch {referenceCode}";
        }
        else if (request.OverallRating == 4)
        {
            scoreDelta = 1.0m;
            eventType = ReputationEvent.PositiveReview;
            reason = $"Khách hàng đánh giá 4 sao hài lòng cho giao dịch {referenceCode}";
        }
        else if (request.OverallRating <= 2)
        {
            scoreDelta = -2.0m;
            eventType = ReputationEvent.NegativeReview;
            reason = $"Khách hàng đánh giá không hài lòng ({request.OverallRating} sao) cho giao dịch {referenceCode}";
        }

        var review = new Review
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            SupplierId = supplierId,
            OrderId = request.OrderId,
            ContractId = request.ContractId,
            OverallRating = request.OverallRating,
            QualityRating = request.QualityRating,
            DeliveryRating = request.DeliveryRating,
            ServiceRating = request.ServiceRating,
            Comment = request.Comment.Trim(),
            IsAnonymous = request.IsAnonymous,
            IsPublic = true,
            IsHiddenByAdmin = false,
            MediaUrls = request.MediaUrls != null && request.MediaUrls.Count > 0 ? JsonSerializer.Serialize(request.MediaUrls) : null,
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow,
            CreatedBy = customerUserId,
            UpdatedBy = customerUserId
        };

        await repository.CreateReviewAsync(review, ct);

        decimal newScore = supplierProfile?.ReputationScore ?? 0m;
        if (scoreDelta != 0m && eventType.HasValue)
        {
            var log = new ReputationLog
            {
                Id = Guid.NewGuid(),
                SupplierId = supplierId,
                EventType = eventType.Value,
                ScoreDelta = scoreDelta,
                Reason = reason,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                CreatedAt = clock.UtcNow,
                CreatedBy = customerUserId,
                UpdatedBy = customerUserId
            };
            await repository.AddReputationLogAsync(log, ct);
            newScore = await repository.UpdateSupplierReputationScoreAsync(supplierId, scoreDelta, ct);
        }

        return new TransactionReviewResponse
        {
            Id = review.Id,
            CustomerId = customer.Id,
            CustomerDisplayName = request.IsAnonymous ? "Khách hàng ẩn danh" : (customer.CompanyName ?? "Doanh nghiệp"),
            CustomerAvatarUrl = request.IsAnonymous ? null : customer.CompanyLogoUrl,
            IsAnonymous = request.IsAnonymous,
            SupplierId = supplierId,
            SupplierName = supplierName,
            OrderId = request.OrderId,
            OrderNumber = orderNumber,
            ContractId = request.ContractId,
            ContractNumber = contractNumber,
            OverallRating = review.OverallRating ?? 5,
            QualityRating = review.QualityRating ?? 5,
            DeliveryRating = review.DeliveryRating ?? 5,
            ServiceRating = review.ServiceRating ?? 5,
            Comment = review.Comment ?? string.Empty,
            MediaUrls = request.MediaUrls ?? new List<string>(),
            ReputationScoreDelta = scoreDelta,
            NewSupplierReputationScore = newScore,
            CreatedAt = review.CreatedAt
        };
    }

    public async Task<TransactionReviewResponse> ReplyTransactionReviewAsync(
        Guid supplierUserId,
        Guid reviewId,
        SupplierReplyRequest request,
        CancellationToken ct = default)
    {
        var supplier = await repository.GetSupplierProfileByUserIdAsync(supplierUserId, ct);
        if (supplier == null)
        {
            throw new HttpException(403, "Tài khoản của bạn không có hồ sơ nhà cung cấp.");
        }

        var review = await repository.GetReviewByIdAsync(reviewId, ct);
        if (review == null)
        {
            throw new HttpException(404, "Đánh giá giao dịch không tồn tại.");
        }

        if (review.SupplierId != supplier.Id)
        {
            throw new HttpException(403, "Bạn không có quyền phản hồi đánh giá của gian hàng khác.");
        }

        if (!string.IsNullOrWhiteSpace(review.SupplierReply))
        {
            throw new HttpException(400, "Nhà cung cấp chỉ được gửi phản hồi chính thức 1 lần duy nhất.");
        }

        review.SupplierReply = request.ReplyContent.Trim();
        review.SupplierRepliedAt = clock.UtcNow;
        review.UpdatedAt = clock.UtcNow;
        review.UpdatedBy = supplierUserId;

        await repository.UpdateReviewAsync(review, ct);

        return new TransactionReviewResponse
        {
            Id = review.Id,
            CustomerId = review.CustomerId,
            CustomerDisplayName = review.IsAnonymous ? "Khách hàng ẩn danh" : "Doanh nghiệp",
            IsAnonymous = review.IsAnonymous,
            SupplierId = review.SupplierId,
            SupplierName = supplier.CompanyName ?? "Nhà cung cấp",
            OrderId = review.OrderId,
            ContractId = review.ContractId,
            OverallRating = review.OverallRating ?? 5,
            QualityRating = review.QualityRating ?? 5,
            DeliveryRating = review.DeliveryRating ?? 5,
            ServiceRating = review.ServiceRating ?? 5,
            Comment = review.Comment ?? string.Empty,
            SupplierReply = review.SupplierReply,
            SupplierRepliedAt = review.SupplierRepliedAt,
            CreatedAt = review.CreatedAt
        };
    }

    public async Task<ShopReviewSummaryResponse> GetShopReviewsAsync(
        string slug,
        int? rating,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var supplier = await repository.GetSupplierByShopSlugAsync(slug, ct);
        if (supplier == null)
        {
            throw new HttpException(404, "Không tìm thấy gian hàng phù hợp.");
        }

        var reviews = await repository.GetShopReviewsAsync(supplier.Id, rating, pageNumber, pageSize, ct);
        var totalCount = await repository.CountShopReviewsAsync(supplier.Id, rating, ct);

        decimal qualityAvg = reviews.Count > 0 ? (decimal)reviews.Average(r => r.QualityRating ?? 5) : 5m;
        decimal deliveryAvg = reviews.Count > 0 ? (decimal)reviews.Average(r => r.DeliveryRating ?? 5) : 5m;
        decimal serviceAvg = reviews.Count > 0 ? (decimal)reviews.Average(r => r.ServiceRating ?? 5) : 5m;
        decimal overallAvg = reviews.Count > 0 ? (decimal)reviews.Average(r => r.OverallRating ?? 5) : 5m;

        var items = reviews.Select(r => new TransactionReviewResponse
        {
            Id = r.Id,
            CustomerId = r.CustomerId,
            CustomerDisplayName = r.IsAnonymous ? "Khách hàng ẩn danh" : "Khách hàng doanh nghiệp",
            IsAnonymous = r.IsAnonymous,
            SupplierId = r.SupplierId,
            SupplierName = supplier.CompanyName ?? "Nhà cung cấp",
            OrderId = r.OrderId,
            ContractId = r.ContractId,
            OverallRating = r.OverallRating ?? 5,
            QualityRating = r.QualityRating ?? 5,
            DeliveryRating = r.DeliveryRating ?? 5,
            ServiceRating = r.ServiceRating ?? 5,
            Comment = r.Comment ?? string.Empty,
            MediaUrls = ParseMediaUrls(r.MediaUrls),
            SupplierReply = r.SupplierReply,
            SupplierRepliedAt = r.SupplierRepliedAt,
            CreatedAt = r.CreatedAt
        }).ToList();

        return new ShopReviewSummaryResponse
        {
            SupplierId = supplier.Id,
            ShopSlug = slug,
            AverageRating = Math.Round(overallAvg, 2),
            TotalReviews = totalCount,
            QualityAverage = Math.Round(qualityAvg, 2),
            DeliveryAverage = Math.Round(deliveryAvg, 2),
            ServiceAverage = Math.Round(serviceAvg, 2),
            Items = items
        };
    }

    public async Task<List<EligibleOrderDto>> GetEligibleOrdersForReviewAsync(Guid customerUserId, CancellationToken ct = default)
    {
        return await repository.GetEligibleOrdersForCustomerAsync(customerUserId, ct);
    }

    public async Task<ProductReviewFeedResponse> GetProductReviewsFeedAsync(
        Guid productId,
        Guid? currentUserId,
        Guid? variantId,
        int? rating,
        bool? hasMedia,
        int pageNumber,
        int pageSize,
        string? sortBy,
        CancellationToken ct = default)
    {
        var product = await repository.GetProductByIdAsync(productId, ct);
        if (product == null)
        {
            throw new HttpException(404, "Sản phẩm không tồn tại.");
        }

        var reviews = await repository.GetProductReviewsFeedAsync(productId, variantId, rating, hasMedia, pageNumber, pageSize, sortBy, ct);
        var totalCount = await repository.CountProductReviewsAsync(productId, variantId, rating, hasMedia, ct);
        var summary = await repository.GetProductReviewStatsAsync(productId, ct);

        HashSet<Guid> userLikedIds = new();
        if (currentUserId.HasValue)
        {
            var customer = await repository.GetCustomerProfileByUserIdAsync(currentUserId.Value, ct);
            var callerId = customer?.Id ?? currentUserId.Value;
            var reviewIds = reviews.Select(r => r.Id).ToList();
            userLikedIds = await repository.GetUserLikedReviewIdsAsync(callerId, reviewIds, ct);
        }

        var items = reviews.Select(r => new ProductReviewItemDto
        {
            Id = r.Id,
            AuthorName = "Khách hàng mua bánh",
            AuthorAvatarUrl = null,
            IsVerifiedPurchase = r.IsVerifiedPurchase,
            VariantId = r.ProductVariantId,
            VariantName = null,
            Rating = r.Rating ?? 5,
            Title = r.Title,
            Content = r.Content ?? string.Empty,
            MediaUrls = ParseMediaUrls(r.MediaUrls),
            LikeCount = r.LikeCount,
            IsLikedByMe = userLikedIds.Contains(r.Id),
            SupplierReply = r.SupplierReply,
            SupplierRepliedAt = r.SupplierRepliedAt,
            CreatedAt = r.CreatedAt
        }).ToList();

        int totalPages = (int)Math.Ceiling((double)totalCount / Math.Max(1, pageSize));

        return new ProductReviewFeedResponse
        {
            ProductId = productId,
            ProductName = product.Name,
            Summary = summary,
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalCount = totalCount,
            HasMore = pageNumber < totalPages
        };
    }

    public async Task<List<EligibleOrderItemDto>> GetEligibleOrderItemsAsync(Guid customerUserId, Guid productId, CancellationToken ct = default)
    {
        return await repository.GetEligibleOrderItemsForProductAsync(customerUserId, productId, ct);
    }

    public async Task<ProductReviewItemDto> CreateProductReviewAsync(
        Guid customerUserId,
        Guid productId,
        CreateProductReviewRequest request,
        CancellationToken ct = default)
    {
        var customer = await repository.GetCustomerProfileByUserIdAsync(customerUserId, ct);
        if (customer == null)
        {
            throw new HttpException(403, "Không tìm thấy hồ sơ khách hàng.");
        }

        var existing = await repository.GetProductReviewByOrderItemAsync(customer.Id, request.OrderItemId, ct);
        if (existing != null)
        {
            throw new HttpException(409, "Quý khách đã gửi đánh giá cho sản phẩm trong đơn hàng này rồi.");
        }

        var eligibleList = await repository.GetEligibleOrderItemsForProductAsync(customerUserId, productId, ct);
        var matched = eligibleList.FirstOrDefault(item => item.OrderItemId == request.OrderItemId);
        if (matched == null)
        {
            throw new HttpException(403, "Chỉ khách hàng đã mua sản phẩm này trong đơn hoàn thành mới có quyền gửi đánh giá.");
        }

        var review = new ProductReview
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            ProductId = productId,
            ProductVariantId = request.ProductVariantId ?? matched.ProductVariantId,
            OrderItemId = request.OrderItemId,
            Rating = request.Rating,
            Title = request.Title?.Trim(),
            Content = request.Content.Trim(),
            MediaUrls = request.MediaUrls != null && request.MediaUrls.Count > 0 ? JsonSerializer.Serialize(request.MediaUrls) : null,
            IsVerifiedPurchase = true,
            LikeCount = 0,
            IsPublic = true,
            IsHiddenByAdmin = false,
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow,
            CreatedBy = customerUserId,
            UpdatedBy = customerUserId
        };

        await repository.CreateProductReviewAsync(review, ct);

        return new ProductReviewItemDto
        {
            Id = review.Id,
            AuthorName = customer.CompanyName ?? "Khách hàng",
            AuthorAvatarUrl = customer.CompanyLogoUrl,
            IsVerifiedPurchase = true,
            VariantId = review.ProductVariantId,
            VariantName = matched.VariantName,
            Rating = review.Rating ?? 5,
            Title = review.Title,
            Content = review.Content,
            MediaUrls = request.MediaUrls ?? new List<string>(),
            LikeCount = 0,
            IsLikedByMe = false,
            CreatedAt = review.CreatedAt
        };
    }

    public async Task<ToggleReviewLikeResponse> ToggleProductReviewLikeAsync(Guid currentUserId, Guid reviewId, CancellationToken ct = default)
    {
        var customer = await repository.GetCustomerProfileByUserIdAsync(currentUserId, ct);
        var callerId = customer?.Id ?? currentUserId;

        var review = await repository.GetProductReviewByIdAsync(reviewId, ct);
        if (review == null)
        {
            throw new HttpException(404, "Đánh giá sản phẩm không tồn tại.");
        }

        var existingLike = await repository.GetReviewLikeAsync(reviewId, callerId, ct);
        bool isLiked;

        if (existingLike == null)
        {
            await repository.AddReviewLikeAsync(new ProductReviewLike
            {
                Id = Guid.NewGuid(),
                ProductReviewId = reviewId,
                CustomerId = callerId,
                CreatedAt = clock.UtcNow,
                CreatedBy = currentUserId,
                UpdatedBy = currentUserId
            }, ct);

            review.LikeCount += 1;
            review.UpdatedAt = clock.UtcNow;
            review.UpdatedBy = currentUserId;
            await repository.UpdateProductReviewAsync(review, ct);
            isLiked = true;
        }
        else
        {
            await repository.RemoveReviewLikeAsync(reviewId, callerId, ct);
            review.LikeCount = Math.Max(0, review.LikeCount - 1);
            review.UpdatedAt = clock.UtcNow;
            review.UpdatedBy = currentUserId;
            await repository.UpdateProductReviewAsync(review, ct);
            isLiked = false;
        }

        return new ToggleReviewLikeResponse
        {
            ReviewId = reviewId,
            IsLiked = isLiked,
            LikeCount = review.LikeCount
        };
    }

    public async Task<ProductReviewItemDto> ReplyProductReviewAsync(
        Guid supplierUserId,
        Guid reviewId,
        SupplierReplyRequest request,
        CancellationToken ct = default)
    {
        var supplier = await repository.GetSupplierProfileByUserIdAsync(supplierUserId, ct);
        if (supplier == null)
        {
            throw new HttpException(403, "Tài khoản của bạn không có hồ sơ nhà cung cấp.");
        }

        var review = await repository.GetProductReviewByIdAsync(reviewId, ct);
        if (review == null)
        {
            throw new HttpException(404, "Đánh giá sản phẩm không tồn tại.");
        }

        var (product, productSupplierId) = await repository.GetProductWithSupplierAsync(review.ProductId, ct);
        if (product == null || productSupplierId != supplier.Id)
        {
            throw new HttpException(403, "Bạn không có quyền phản hồi sản phẩm của gian hàng khác.");
        }

        if (!string.IsNullOrWhiteSpace(review.SupplierReply))
        {
            throw new HttpException(400, "Nhà cung cấp chỉ được gửi phản hồi 1 lần duy nhất.");
        }

        review.SupplierReply = request.ReplyContent.Trim();
        review.SupplierRepliedAt = clock.UtcNow;
        review.UpdatedAt = clock.UtcNow;
        review.UpdatedBy = supplierUserId;

        await repository.UpdateProductReviewAsync(review, ct);

        return new ProductReviewItemDto
        {
            Id = review.Id,
            AuthorName = "Khách hàng mua bánh",
            IsVerifiedPurchase = review.IsVerifiedPurchase,
            VariantId = review.ProductVariantId,
            Rating = review.Rating ?? 5,
            Title = review.Title,
            Content = review.Content ?? string.Empty,
            MediaUrls = ParseMediaUrls(review.MediaUrls),
            LikeCount = review.LikeCount,
            SupplierReply = review.SupplierReply,
            SupplierRepliedAt = review.SupplierRepliedAt,
            CreatedAt = review.CreatedAt
        };
    }

    public async Task<SupplierReputationResponse> GetSupplierReputationAsync(Guid supplierId, CancellationToken ct = default)
    {
        var supplier = await repository.GetSupplierProfileAsync(supplierId, ct);
        if (supplier == null)
        {
            throw new HttpException(404, "Nhà cung cấp không tồn tại.");
        }

        var logs = await repository.GetReputationLogsAsync(supplierId, 20, ct);
        var (tier, title, color) = ReputationBadgeHelper.ResolveBadge(supplier.ReputationScore);

        return new SupplierReputationResponse
        {
            SupplierId = supplier.Id,
            CompanyName = supplier.CompanyName ?? "Nhà cung cấp",
            ReputationScore = supplier.ReputationScore,
            BadgeTier = tier,
            BadgeTitle = title,
            BadgeColor = color,
            Stats = new SupplierStatsDto
            {
                TotalContracts = supplier.TotalContracts,
                ContractsOnTime = supplier.ContractsOnTime,
                ContractsLate = supplier.ContractsLate,
                ContractsBreached = supplier.ContractsBreached,
                AverageRating = supplier.AvgRating,
                PositiveReviewsCount = logs.Count(l => l.EventType == ReputationEvent.PositiveReview),
                NegativeReviewsCount = logs.Count(l => l.EventType == ReputationEvent.NegativeReview)
            },
            RecentLogs = logs.Select(l => new ReputationLogDto
            {
                Id = l.Id,
                EventType = l.EventType.ToString(),
                ScoreDelta = l.ScoreDelta,
                Reason = l.Reason,
                ReferenceId = l.ReferenceId,
                ReferenceType = l.ReferenceType,
                CreatedAt = l.CreatedAt
            }).ToList()
        };
    }

    public async Task<SupplierReputationResponse> AdminAdjustReputationAsync(
        Guid adminUserId,
        Guid supplierId,
        AdminAdjustReputationRequest request,
        CancellationToken ct = default)
    {
        var supplier = await repository.GetSupplierProfileAsync(supplierId, ct);
        if (supplier == null)
        {
            throw new HttpException(404, "Nhà cung cấp không tồn tại.");
        }

        var log = new ReputationLog
        {
            Id = Guid.NewGuid(),
            SupplierId = supplierId,
            EventType = request.EventType,
            ScoreDelta = request.ScoreDelta,
            Reason = request.Reason.Trim(),
            ReferenceId = request.ReferenceId,
            ReferenceType = request.ReferenceType ?? "AdminAction",
            CreatedAt = clock.UtcNow,
            CreatedBy = adminUserId,
            UpdatedBy = adminUserId
        };

        await repository.AddReputationLogAsync(log, ct);
        await repository.UpdateSupplierReputationScoreAsync(supplierId, request.ScoreDelta, ct);

        return await GetSupplierReputationAsync(supplierId, ct);
    }

    public async Task<bool> ModerateReviewAsync(
        Guid adminUserId,
        Guid reviewId,
        bool isProductReview,
        ModerateReviewRequest request,
        CancellationToken ct = default)
    {
        if (isProductReview)
        {
            var productReview = await repository.GetProductReviewByIdAsync(reviewId, ct);
            if (productReview == null)
            {
                throw new HttpException(404, "Đánh giá sản phẩm không tồn tại.");
            }

            productReview.IsHiddenByAdmin = request.IsHidden;
            productReview.IsPublic = !request.IsHidden;
            productReview.UpdatedAt = clock.UtcNow;
            productReview.UpdatedBy = adminUserId;
            await repository.UpdateProductReviewAsync(productReview, ct);
        }
        else
        {
            var txReview = await repository.GetReviewByIdAsync(reviewId, ct);
            if (txReview == null)
            {
                throw new HttpException(404, "Đánh giá giao dịch không tồn tại.");
            }

            txReview.IsHiddenByAdmin = request.IsHidden;
            txReview.IsPublic = !request.IsHidden;
            txReview.UpdatedAt = clock.UtcNow;
            txReview.UpdatedBy = adminUserId;
            await repository.UpdateReviewAsync(txReview, ct);
        }

        return true;
    }

    private static List<string> ParseMediaUrls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
