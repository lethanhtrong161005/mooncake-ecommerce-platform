namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

public sealed class ReviewRepository(ApplicationDbContext context) : IReviewRepository
{
    // Transaction Reviews
    public async Task<Review> CreateReviewAsync(Review review, CancellationToken ct = default)
    {
        await context.Reviews.AddAsync(review, ct);
        await context.SaveChangesAsync(ct);
        return review;
    }

    public async Task<Review?> GetReviewByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Reviews.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);

    public async Task<Review?> GetReviewByOrderIdAsync(Guid orderId, CancellationToken ct = default) =>
        await context.Reviews.FirstOrDefaultAsync(r => r.OrderId == orderId && !r.IsDeleted, ct);

    public async Task<Review?> GetReviewByContractIdAsync(Guid contractId, CancellationToken ct = default) =>
        await context.Reviews.FirstOrDefaultAsync(r => r.ContractId == contractId && !r.IsDeleted, ct);

    public async Task<List<Review>> GetShopReviewsAsync(Guid supplierId, int? rating, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var query = context.Reviews.AsNoTracking()
            .Where(r => r.SupplierId == supplierId && !r.IsDeleted && !r.IsHiddenByAdmin);

        if (rating.HasValue)
        {
            query = query.Where(r => r.OverallRating == rating.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountShopReviewsAsync(Guid supplierId, int? rating, CancellationToken ct = default)
    {
        var query = context.Reviews.AsNoTracking()
            .Where(r => r.SupplierId == supplierId && !r.IsDeleted && !r.IsHiddenByAdmin);

        if (rating.HasValue)
        {
            query = query.Where(r => r.OverallRating == rating.Value);
        }

        return await query.CountAsync(ct);
    }

    public async Task UpdateReviewAsync(Review review, CancellationToken ct = default)
    {
        context.Reviews.Update(review);
        await context.SaveChangesAsync(ct);
    }

    // Product Reviews
    public async Task<ProductReview> CreateProductReviewAsync(ProductReview review, CancellationToken ct = default)
    {
        await context.ProductReviews.AddAsync(review, ct);
        await context.SaveChangesAsync(ct);
        return review;
    }

    public async Task<ProductReview?> GetProductReviewByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.ProductReviews.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);

    public async Task<ProductReview?> GetProductReviewByOrderItemAsync(Guid customerId, Guid orderItemId, CancellationToken ct = default) =>
        await context.ProductReviews.FirstOrDefaultAsync(r => r.CustomerId == customerId && r.OrderItemId == orderItemId && !r.IsDeleted, ct);

    public async Task<List<ProductReview>> GetProductReviewsFeedAsync(
        Guid productId,
        Guid? variantId,
        int? rating,
        bool? hasMedia,
        int pageNumber,
        int pageSize,
        string? sortBy,
        CancellationToken ct = default)
    {
        var query = context.ProductReviews.AsNoTracking()
            .Where(r => r.ProductId == productId && !r.IsDeleted && !r.IsHiddenByAdmin);

        if (variantId.HasValue)
        {
            query = query.Where(r => r.ProductVariantId == variantId.Value);
        }

        if (rating.HasValue)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        if (hasMedia == true)
        {
            query = query.Where(r => r.MediaUrls != null && r.MediaUrls != "[]" && r.MediaUrls != "");
        }

        if (sortBy == "newest")
        {
            query = query.OrderByDescending(r => r.CreatedAt);
        }
        else
        {
            query = query.OrderByDescending(r => r.LikeCount).ThenByDescending(r => r.CreatedAt);
        }

        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountProductReviewsAsync(Guid productId, Guid? variantId, int? rating, bool? hasMedia, CancellationToken ct = default)
    {
        var query = context.ProductReviews.AsNoTracking()
            .Where(r => r.ProductId == productId && !r.IsDeleted && !r.IsHiddenByAdmin);

        if (variantId.HasValue)
        {
            query = query.Where(r => r.ProductVariantId == variantId.Value);
        }

        if (rating.HasValue)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        if (hasMedia == true)
        {
            query = query.Where(r => r.MediaUrls != null && r.MediaUrls != "[]" && r.MediaUrls != "");
        }

        return await query.CountAsync(ct);
    }

    public async Task<ProductReviewSummaryDto> GetProductReviewStatsAsync(Guid productId, CancellationToken ct = default)
    {
        var reviews = await context.ProductReviews.AsNoTracking()
            .Where(r => r.ProductId == productId && !r.IsDeleted && !r.IsHiddenByAdmin)
            .Select(r => new { r.Rating, r.IsVerifiedPurchase, HasMedia = r.MediaUrls != null && r.MediaUrls != "[]" && r.MediaUrls != "" })
            .ToListAsync(ct);

        if (reviews.Count == 0)
        {
            return new ProductReviewSummaryDto
            {
                AverageRating = 5.0m,
                TotalReviews = 0,
                VerifiedPurchaseCount = 0,
                WithMediaCount = 0,
                StarDistribution = new StarDistributionDto()
            };
        }

        int s5 = reviews.Count(r => r.Rating == 5);
        int s4 = reviews.Count(r => r.Rating == 4);
        int s3 = reviews.Count(r => r.Rating == 3);
        int s2 = reviews.Count(r => r.Rating == 2);
        int s1 = reviews.Count(r => r.Rating == 1);

        decimal avg = (decimal)reviews.Average(r => r.Rating ?? 5);

        return new ProductReviewSummaryDto
        {
            AverageRating = Math.Round(avg, 2),
            TotalReviews = reviews.Count,
            VerifiedPurchaseCount = reviews.Count(r => r.IsVerifiedPurchase),
            WithMediaCount = reviews.Count(r => r.HasMedia),
            StarDistribution = new StarDistributionDto
            {
                Star5 = s5,
                Star4 = s4,
                Star3 = s3,
                Star2 = s2,
                Star1 = s1
            }
        };
    }

    public async Task UpdateProductReviewAsync(ProductReview review, CancellationToken ct = default)
    {
        context.ProductReviews.Update(review);
        await context.SaveChangesAsync(ct);
    }

    // Likes
    public async Task<ProductReviewLike?> GetReviewLikeAsync(Guid reviewId, Guid customerId, CancellationToken ct = default) =>
        await context.ProductReviewLikes.FirstOrDefaultAsync(l => l.ProductReviewId == reviewId && l.CustomerId == customerId && !l.IsDeleted, ct);

    public async Task AddReviewLikeAsync(ProductReviewLike like, CancellationToken ct = default)
    {
        await context.ProductReviewLikes.AddAsync(like, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveReviewLikeAsync(Guid reviewId, Guid customerId, CancellationToken ct = default)
    {
        var existing = await context.ProductReviewLikes
            .FirstOrDefaultAsync(l => l.ProductReviewId == reviewId && l.CustomerId == customerId, ct);
        if (existing != null)
        {
            context.ProductReviewLikes.Remove(existing);
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task<HashSet<Guid>> GetUserLikedReviewIdsAsync(Guid customerId, List<Guid> reviewIds, CancellationToken ct = default)
    {
        if (reviewIds.Count == 0) return new HashSet<Guid>();

        var liked = await context.ProductReviewLikes.AsNoTracking()
            .Where(l => l.CustomerId == customerId && reviewIds.Contains(l.ProductReviewId) && !l.IsDeleted)
            .Select(l => l.ProductReviewId)
            .ToListAsync(ct);

        return liked.ToHashSet();
    }

    // Reputation & Profiles
    public async Task<ReputationLog> AddReputationLogAsync(ReputationLog log, CancellationToken ct = default)
    {
        await context.ReputationLogs.AddAsync(log, ct);
        await context.SaveChangesAsync(ct);
        return log;
    }

    public async Task<List<ReputationLog>> GetReputationLogsAsync(Guid supplierId, int limit, CancellationToken ct = default) =>
        await context.ReputationLogs.AsNoTracking()
            .Where(l => l.SupplierId == supplierId && !l.IsDeleted)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<decimal> UpdateSupplierReputationScoreAsync(Guid supplierId, decimal scoreDelta, CancellationToken ct = default)
    {
        var supplier = await context.SupplierProfiles.FirstOrDefaultAsync(s => s.Id == supplierId, ct);
        if (supplier == null) return 0m;

        supplier.ReputationScore = Math.Max(0m, supplier.ReputationScore + scoreDelta);

        var avg = await context.Reviews.AsNoTracking()
            .Where(r => r.SupplierId == supplierId && !r.IsDeleted && !r.IsHiddenByAdmin)
            .Select(r => (decimal?)r.OverallRating)
            .AverageAsync(ct);

        if (avg.HasValue)
        {
            supplier.AvgRating = Math.Round(avg.Value, 2);
        }

        context.SupplierProfiles.Update(supplier);
        await context.SaveChangesAsync(ct);

        return supplier.ReputationScore;
    }

    public async Task<SupplierProfile?> GetSupplierProfileAsync(Guid supplierId, CancellationToken ct = default) =>
        await context.SupplierProfiles.AsNoTracking().FirstOrDefaultAsync(s => s.Id == supplierId && !s.IsDeleted, ct);

    public async Task<SupplierProfile?> GetSupplierProfileByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await context.SupplierProfiles.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted, ct);

    public async Task<SupplierProfile?> GetSupplierByShopSlugAsync(string slug, CancellationToken ct = default)
    {
        var shop = await context.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slug && !s.IsDeleted, ct);
        if (shop == null) return null;

        return await context.SupplierProfiles.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shop.SupplierId && !s.IsDeleted, ct);
    }

    public async Task<CustomerProfile?> GetCustomerProfileByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await context.CustomerProfiles.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted, ct);

    public async Task<Product?> GetProductByIdAsync(Guid productId, CancellationToken ct = default) =>
        await context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, ct);

    public async Task<(Product? Product, Guid? SupplierId)> GetProductWithSupplierAsync(Guid productId, CancellationToken ct = default)
    {
        var result = await (
            from p in context.Products.AsNoTracking()
            where p.Id == productId && !p.IsDeleted
            join s in context.Shops.AsNoTracking() on p.ShopId equals s.Id
            select new { Product = p, s.SupplierId }
        ).FirstOrDefaultAsync(ct);

        if (result == null) return (null, null);
        return (result.Product, result.SupplierId);
    }

    // Eligibility & Context
    public async Task<(Order? Order, Guid? SupplierId)> GetOrderWithSupplierForCustomerAsync(Guid customerUserId, Guid orderId, CancellationToken ct = default)
    {
        var customer = await context.CustomerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == customerUserId && !c.IsDeleted, ct);
        if (customer == null) return (null, null);

        var result = await (
            from o in context.Orders.AsNoTracking()
            where o.Id == orderId && o.CustomerId == customer.Id && !o.IsDeleted
            join s in context.Shops.AsNoTracking() on o.ShopId equals s.Id
            select new { Order = o, s.SupplierId }
        ).FirstOrDefaultAsync(ct);

        if (result == null) return (null, null);
        return (result.Order, result.SupplierId);
    }

    public async Task<Contract?> GetContractForCustomerAsync(Guid customerUserId, Guid contractId, CancellationToken ct = default)
    {
        var customer = await context.CustomerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == customerUserId && !c.IsDeleted, ct);
        if (customer == null) return null;

        return await context.Contracts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contractId && c.CustomerId == customer.Id && !c.IsDeleted, ct);
    }

    public async Task<List<EligibleOrderDto>> GetEligibleOrdersForCustomerAsync(Guid customerUserId, CancellationToken ct = default)
    {
        var customer = await context.CustomerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == customerUserId && !c.IsDeleted, ct);
        if (customer == null) return new List<EligibleOrderDto>();

        var reviewedOrderIds = await context.Reviews.AsNoTracking()
            .Where(r => r.CustomerId == customer.Id && r.OrderId != null && !r.IsDeleted)
            .Select(r => r.OrderId!.Value)
            .ToListAsync(ct);

        var orders = await (
            from o in context.Orders.AsNoTracking()
            where o.CustomerId == customer.Id
                  && o.Status == OrderStatus.Delivered
                  && !o.IsDeleted
                  && !reviewedOrderIds.Contains(o.Id)
            join s in context.Shops.AsNoTracking() on o.ShopId equals s.Id
            join sp in context.SupplierProfiles.AsNoTracking() on s.SupplierId equals sp.Id into spJoin
            from sp in spJoin.DefaultIfEmpty()
            orderby o.UpdatedAt descending
            select new EligibleOrderDto
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                SupplierId = s.SupplierId,
                SupplierName = sp != null ? (sp.CompanyName ?? "Nhà cung cấp") : "Nhà cung cấp",
                TotalAmount = o.TotalAmount,
                DeliveredAt = o.UpdatedAt
            }
        ).Take(50).ToListAsync(ct);

        return orders;
    }

    public async Task<List<EligibleOrderItemDto>> GetEligibleOrderItemsForProductAsync(Guid customerUserId, Guid productId, CancellationToken ct = default)
    {
        var customer = await context.CustomerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == customerUserId && !c.IsDeleted, ct);
        if (customer == null) return new List<EligibleOrderItemDto>();

        var reviewedOrderItemIds = await context.ProductReviews.AsNoTracking()
            .Where(r => r.CustomerId == customer.Id && !r.IsDeleted)
            .Select(r => r.OrderItemId)
            .ToListAsync(ct);

        var items = await (
            from oi in context.OrderItems.AsNoTracking()
            where oi.ProductId == productId && !oi.IsDeleted && !reviewedOrderItemIds.Contains(oi.Id)
            join o in context.Orders.AsNoTracking() on oi.OrderId equals o.Id
            where o.CustomerId == customer.Id && o.Status == OrderStatus.Delivered && !o.IsDeleted
            join p in context.Products.AsNoTracking() on oi.ProductId equals p.Id
            join pv in context.ProductVariants.AsNoTracking() on oi.VariantId equals pv.Id into pvJoin
            from pv in pvJoin.DefaultIfEmpty()
            orderby o.UpdatedAt descending
            select new EligibleOrderItemDto
            {
                OrderItemId = oi.Id,
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                ProductId = p.Id,
                ProductName = p.Name,
                ProductVariantId = oi.VariantId,
                VariantName = pv != null ? pv.Name : oi.VariantNameSnapshot,
                DeliveredAt = o.UpdatedAt
            }
        ).Take(50).ToListAsync(ct);

        return items;
    }
}
