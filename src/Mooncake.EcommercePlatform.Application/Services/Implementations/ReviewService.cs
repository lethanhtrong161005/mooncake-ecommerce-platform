namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Text.Json;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;

public sealed class ReviewService(IReviewRepository repository, IDateTimeProvider clock) : IReviewService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ReviewResponse> CreateTransactionReviewAsync(Guid customerId, CreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OrderId.HasValue == request.ContractId.HasValue)
            throw new HttpException(400, "Provide exactly one order ID or contract ID.");
        var review = new Review
        {
            Id = Guid.NewGuid(), CustomerId = customerId, OrderId = request.OrderId, ContractId = request.ContractId,
            OverallRating = request.OverallRating, QualityRating = request.QualityRating,
            DeliveryRating = request.DeliveryRating, ServiceRating = request.ServiceRating,
            Comment = request.Comment?.Trim(), MediaUrls = JsonSerializer.Serialize(request.MediaUrls.Select(uri => uri.ToString()), JsonOptions),
            IsAnonymous = request.IsAnonymous, IsPublic = true, CreatedAt = clock.UtcNow, CreatedBy = customerId
        };
        var saved = await repository.CreateTransactionReviewAsync(customerId, review, cancellationToken)
                     ?? throw new HttpException(409, "The transaction is not eligible for review or has already been reviewed.");
        return ToResponse(saved);
    }

    public async Task<IReadOnlyList<ReviewResponse>> GetSupplierReviewsAsync(Guid supplierId,
        CancellationToken cancellationToken = default) =>
        (await repository.GetSupplierReviewsAsync(supplierId, cancellationToken)).Select(ToResponse).ToArray();

    public async Task ReplyToReviewAsync(Guid supplierUserId, Guid reviewId, string reply,
        CancellationToken cancellationToken = default)
    {
        if (!await repository.ReplyToReviewAsync(reviewId, supplierUserId, reply.Trim(), cancellationToken))
            throw new HttpException(409, "The review was not found, is not owned by this supplier, or already has a reply.");
    }

    public async Task ModerateReviewAsync(Guid adminUserId, Guid reviewId, ModerateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await repository.ModerateReviewAsync(reviewId, adminUserId, request.IsPublic, request.Reason?.Trim(), cancellationToken))
            throw new HttpException(404, "Review was not found.");
    }

    public async Task<ProductReviewSummary> GetProductReviewsAsync(Guid productId, Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await repository.GetProductReviewsAsync(productId, cancellationToken);
        var liked = userId.HasValue
            ? await repository.GetLikedProductReviewIdsAsync(userId.Value, reviews.Select(review => review.Id).ToArray(), cancellationToken)
            : new HashSet<Guid>();
        var counts = reviews.Where(review => review.Rating.HasValue).GroupBy(review => (int)review.Rating!.Value)
            .ToDictionary(group => group.Key, group => group.Count());
        var distribution = Enumerable.Range(1, 5).ToDictionary(star => star, star => counts.GetValueOrDefault(star));
        var average = reviews.Count == 0 ? 0m : (decimal)reviews.Average(review => (double)(review.Rating ?? 0));
        var results = reviews.Select(review => ToProductResponse(review, liked.Contains(review.Id))).ToArray();
        return new ProductReviewSummary(average, reviews.Count, distribution, results);
    }

    public async Task AddProductReviewAsync(Guid customerId, Guid productId, CreateProductReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var review = new ProductReview
        {
            Id = Guid.NewGuid(), CustomerId = customerId, ProductId = productId, OrderItemId = request.OrderItemId,
            Rating = request.Rating, Title = request.Title?.Trim(), Content = request.Content?.Trim(),
            MediaUrls = JsonSerializer.Serialize(request.MediaUrls.Select(uri => uri.ToString()), JsonOptions),
            IsVerifiedPurchase = true, IsPublic = true, CreatedAt = clock.UtcNow, CreatedBy = customerId
        };
        if (!await repository.CreateProductReviewAsync(customerId, review, cancellationToken))
            throw new HttpException(409, "A delivered purchase of this product was not found or has already been reviewed.");
    }

    public async Task SetProductReviewLikeAsync(Guid customerId, Guid reviewId, bool liked,
        CancellationToken cancellationToken = default)
    {
        if (!await repository.SetProductReviewLikeAsync(reviewId, customerId, liked, cancellationToken))
            throw new HttpException(404, "Product review was not found.");
    }

    public async Task ReplyToProductReviewAsync(Guid supplierUserId, Guid reviewId, string reply,
        CancellationToken cancellationToken = default)
    {
        if (!await repository.ReplyToProductReviewAsync(reviewId, supplierUserId, reply.Trim(), cancellationToken))
            throw new HttpException(409, "The review was not found, is not owned by this supplier, or already has a reply.");
    }

    public async Task ModerateProductReviewAsync(Guid adminUserId, Guid reviewId, ModerateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await repository.ModerateProductReviewAsync(reviewId, adminUserId, request.IsPublic,
                request.Reason?.Trim(), cancellationToken))
            throw new HttpException(404, "Product review was not found.");
    }

    private static ReviewResponse ToResponse(Review review) => new(review.Id, review.SupplierId, review.OrderId,
        review.ContractId, review.OverallRating, review.QualityRating, review.DeliveryRating, review.ServiceRating,
        review.Comment, ParseMedia(review.MediaUrls), review.IsAnonymous, review.SupplierReply,
        review.SupplierRepliedAt, review.IsPublic && !review.IsHiddenByAdmin, review.CreatedAt);

    private static ProductReviewResponse ToProductResponse(ProductReview review, bool liked) => new(review.Id,
        review.ProductId, review.ProductVariantId, review.OrderItemId, review.Rating, review.Title, review.Content,
        ParseMedia(review.MediaUrls), review.IsVerifiedPurchase, review.LikeCount, liked, review.SupplierReply,
        review.SupplierRepliedAt, review.CreatedAt);

    private static IReadOnlyList<string> ParseMedia(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];
        try { return JsonSerializer.Deserialize<string[]>(value, JsonOptions) ?? []; }
        catch (JsonException) { return []; }
    }
}
