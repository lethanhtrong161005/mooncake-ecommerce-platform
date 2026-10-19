namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

public sealed class ReviewRepository(ApplicationDbContext context, IDateTimeProvider clock) : IReviewRepository
{
    public async Task<Review?> CreateTransactionReviewAsync(Guid customerId, Review review,
        CancellationToken cancellationToken = default)
    {
        Guid supplierId;
        if (review.OrderId is Guid orderId)
        {
            var order = await (from item in context.Orders.AsNoTracking()
                join shop in context.Shops.AsNoTracking() on item.ShopId equals shop.Id
                where item.Id == orderId && item.CustomerId == customerId && item.Status == OrderStatus.Delivered
                    && !item.IsDeleted && !shop.IsDeleted
                select shop.SupplierId).SingleOrDefaultAsync(cancellationToken);
            supplierId = order;
        }
        else if (review.ContractId is Guid contractId)
        {
            supplierId = await context.Contracts.AsNoTracking()
                .Where(contract => contract.Id == contractId && contract.CustomerId == customerId
                    && (contract.Status == ContractStatus.CompletedLate || contract.Status == ContractStatus.CompletedOnTime)
                    && !contract.IsDeleted)
                .Select(contract => contract.SupplierId).SingleOrDefaultAsync(cancellationToken);
        }
        else return null;

        if (supplierId == Guid.Empty) return null;
        review.SupplierId = supplierId;
        context.Reviews.Add(review);
        var profile = await context.SupplierProfiles.FirstAsync(profile => profile.Id == supplierId, cancellationToken);
        var average = await context.Reviews.Where(item => item.SupplierId == supplierId && item.IsPublic
                && !item.IsHiddenByAdmin && !item.IsDeleted && item.OverallRating.HasValue)
            .Select(item => (decimal)item.OverallRating!.Value).ToListAsync(cancellationToken);
        var newAverage = (average.Sum() + review.OverallRating!.Value) / (average.Count + 1);
        profile.AvgRating = newAverage;
        var delta = newAverage >= 4m ? 1m : newAverage <= 2m ? -1m : 0m;
        if (delta != 0)
        {
            profile.ReputationScore += delta;
            context.ReputationLogs.Add(new ReputationLog
            {
                SupplierId = supplierId, EventType = delta > 0 ? ReputationEvent.PositiveReview : ReputationEvent.NegativeReview,
                ScoreDelta = delta, Reason = $"Transaction review rating: {review.OverallRating}/5.",
                ReferenceId = review.Id, ReferenceType = "review", CreatedBy = customerId
            });
        }
        await context.SaveChangesAsync(cancellationToken);
        return review;
    }

    public async Task<IReadOnlyList<Review>> GetSupplierReviewsAsync(Guid supplierId,
        CancellationToken cancellationToken = default) => await context.Reviews.AsNoTracking()
        .Where(review => review.SupplierId == supplierId && review.IsPublic && !review.IsHiddenByAdmin && !review.IsDeleted)
        .OrderByDescending(review => review.CreatedAt).Take(200).ToListAsync(cancellationToken);

    public async Task<bool> ReplyToReviewAsync(Guid reviewId, Guid supplierUserId, string reply,
        CancellationToken cancellationToken = default)
    {
        var review = await (from item in context.Reviews
            join profile in context.SupplierProfiles on item.SupplierId equals profile.Id
            where item.Id == reviewId && profile.UserId == supplierUserId && !item.IsDeleted && !profile.IsDeleted
            select item).FirstOrDefaultAsync(cancellationToken);
        if (review is null || review.SupplierReply is not null) return false;
        review.SupplierReply = reply;
        review.SupplierRepliedAt = clock.UtcNow;
        review.UpdatedAt = clock.UtcNow;
        review.UpdatedBy = supplierUserId;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ModerateReviewAsync(Guid reviewId, Guid adminUserId, bool isPublic, string? reason,
        CancellationToken cancellationToken = default)
    {
        var review = await context.Reviews.FirstOrDefaultAsync(item => item.Id == reviewId && !item.IsDeleted, cancellationToken);
        if (review is null) return false;
        review.IsPublic = isPublic;
        review.IsHiddenByAdmin = !isPublic;
        review.UpdatedAt = clock.UtcNow;
        review.UpdatedBy = adminUserId;
        context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminUserId, Action = isPublic ? "review.unhide" : "review.hide",
            EntityName = "Review", EntityId = reviewId.ToString(), MetadataJson = reason,
            CreatedBy = adminUserId
        });
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CreateProductReviewAsync(Guid customerId, ProductReview review,
        CancellationToken cancellationToken = default)
    {
        var purchase = await (from item in context.OrderItems.AsNoTracking()
            join order in context.Orders.AsNoTracking() on item.OrderId equals order.Id
            where item.Id == review.OrderItemId && item.ProductId == review.ProductId && order.CustomerId == customerId
                && order.Status == OrderStatus.Delivered && !item.IsDeleted && !order.IsDeleted
            select new { item.VariantId }).SingleOrDefaultAsync(cancellationToken);
        if (purchase is null) return false;
        review.CustomerId = customerId;
        review.ProductVariantId = purchase.VariantId;
        context.ProductReviews.Add(review);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ProductReview>> GetProductReviewsAsync(Guid productId,
        CancellationToken cancellationToken = default) => await context.ProductReviews.AsNoTracking()
        .Where(review => review.ProductId == productId && review.IsPublic && !review.IsHiddenByAdmin && !review.IsDeleted)
        .OrderByDescending(review => review.CreatedAt).Take(200).ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetLikedProductReviewIdsAsync(Guid customerId,
        IReadOnlyCollection<Guid> reviewIds, CancellationToken cancellationToken = default) => (await context.ProductReviewLikes
        .Where(like => like.CustomerId == customerId && reviewIds.Contains(like.ProductReviewId) && !like.IsDeleted)
        .Select(like => like.ProductReviewId).ToListAsync(cancellationToken)).ToHashSet();

    public async Task<bool> SetProductReviewLikeAsync(Guid reviewId, Guid customerId, bool liked,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);
        var review = await context.ProductReviews.FromSqlInterpolated(
                $"SELECT * FROM product_reviews WHERE id = {reviewId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        if (review is null || review.IsDeleted || !review.IsPublic || review.IsHiddenByAdmin) return false;
        var existing = await context.ProductReviewLikes.FirstOrDefaultAsync(item => item.ProductReviewId == reviewId
            && item.CustomerId == customerId, cancellationToken);
        if (liked && existing is null)
        {
            context.ProductReviewLikes.Add(new ProductReviewLike { Id = Guid.NewGuid(), ProductReviewId = reviewId,
                CustomerId = customerId, CreatedBy = customerId });
            review.LikeCount++;
        }
        else if (liked && existing is { IsDeleted: true })
        {
            existing.IsDeleted = false;
            existing.UpdatedAt = clock.UtcNow;
            existing.UpdatedBy = customerId;
            review.LikeCount++;
        }
        else if (!liked && existing is not null)
        {
            existing.IsDeleted = true;
            review.LikeCount = Math.Max(0, review.LikeCount - 1);
        }
        await context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReplyToProductReviewAsync(Guid reviewId, Guid supplierUserId, string reply,
        CancellationToken cancellationToken = default)
    {
        var review = await (from item in context.ProductReviews
            join product in context.Products on item.ProductId equals product.Id
            join shop in context.Shops on product.ShopId equals shop.Id
            join profile in context.SupplierProfiles on shop.SupplierId equals profile.Id
            where item.Id == reviewId && profile.UserId == supplierUserId && !item.IsDeleted && !profile.IsDeleted
            select item).FirstOrDefaultAsync(cancellationToken);
        if (review is null || review.SupplierReply is not null) return false;
        review.SupplierReply = reply;
        review.SupplierRepliedAt = clock.UtcNow;
        review.UpdatedAt = clock.UtcNow;
        review.UpdatedBy = supplierUserId;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ModerateProductReviewAsync(Guid reviewId, Guid adminUserId, bool isPublic, string? reason,
        CancellationToken cancellationToken = default)
    {
        var review = await context.ProductReviews.FirstOrDefaultAsync(item => item.Id == reviewId && !item.IsDeleted, cancellationToken);
        if (review is null) return false;
        review.IsPublic = isPublic;
        review.IsHiddenByAdmin = !isPublic;
        review.UpdatedAt = clock.UtcNow;
        review.UpdatedBy = adminUserId;
        context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminUserId, Action = isPublic ? "product_review.unhide" : "product_review.hide",
            EntityName = "ProductReview", EntityId = reviewId.ToString(), MetadataJson = reason,
            CreatedBy = adminUserId
        });
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
