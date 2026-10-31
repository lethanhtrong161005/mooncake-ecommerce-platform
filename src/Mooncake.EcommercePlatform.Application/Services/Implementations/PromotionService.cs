namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Models;
using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements supplier-owned quantity-tier promotions.</summary>
public sealed class PromotionService(IPromotionRepository repository, IDateTimeProvider dateTimeProvider) : IPromotionService
{
    public async Task<IReadOnlyList<PromotionResponse>> GetMineAsync(Guid supplierUserId, CancellationToken cancellationToken = default)
    {
        var shopId = await GetVerifiedShopIdAsync(supplierUserId, cancellationToken);
        var promotions = await repository.GetForShopAsync(shopId, cancellationToken);
        return promotions.Select(PromotionResponseHelper.ToResponse).ToList();
    }

    public async Task<PromotionResponse> CreateAsync(Guid supplierUserId, SavePromotionRequest request, CancellationToken cancellationToken = default)
    {
        var shopId = await GetVerifiedShopIdAsync(supplierUserId, cancellationToken);
        await ValidateAsync(shopId, request, null, cancellationToken);
        var promotion = BuildPromotion(shopId, request, supplierUserId);
        await repository.SaveAsync(promotion, cancellationToken);
        return PromotionResponseHelper.ToResponse(promotion);
    }

    public async Task<PromotionResponse> UpdateAsync(Guid supplierUserId, Guid promotionId, SavePromotionRequest request, CancellationToken cancellationToken = default)
    {
        var shopId = await GetVerifiedShopIdAsync(supplierUserId, cancellationToken);
        var promotion = await repository.GetAsync(promotionId, cancellationToken)
                        ?? throw new HttpException(404, "Promotion was not found.");
        if (promotion.ShopId != shopId)
            throw new HttpException(404, "Promotion was not found.");
        await ValidateAsync(shopId, request, promotionId, cancellationToken);
        promotion = promotion with
        {
            Name = request.Name.Trim(), Description = request.Description?.Trim(), ProductId = request.ProductId,
            MinQty = request.MinQty, MaxQty = request.MaxQty, DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue, StartDateUtc = request.StartAtUtc?.UtcDateTime,
            EndDateUtc = request.EndAtUtc?.UtcDateTime, IsActive = true, UpdatedBy = supplierUserId,
            UpdatedAtUtc = dateTimeProvider.UtcNow
        };
        await repository.SaveAsync(promotion, cancellationToken);
        return PromotionResponseHelper.ToResponse(promotion);
    }

    public async Task DeactivateAsync(Guid supplierUserId, Guid promotionId, CancellationToken cancellationToken = default)
    {
        var shopId = await GetVerifiedShopIdAsync(supplierUserId, cancellationToken);
        var promotion = await repository.GetAsync(promotionId, cancellationToken)
                        ?? throw new HttpException(404, "Promotion was not found.");
        if (promotion.ShopId != shopId)
            throw new HttpException(404, "Promotion was not found.");
        promotion = promotion with { IsActive = false, UpdatedBy = supplierUserId, UpdatedAtUtc = dateTimeProvider.UtcNow };
        await repository.SaveAsync(promotion, cancellationToken);
    }

    private async Task<Guid> GetVerifiedShopIdAsync(Guid supplierUserId, CancellationToken cancellationToken) =>
        await repository.GetVerifiedShopIdAsync(supplierUserId, cancellationToken)
        ?? throw new HttpException(403, "A verified supplier with an active shop is required.");

    private async Task ValidateAsync(Guid shopId, SavePromotionRequest request, Guid? exceptPromotionId, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.DiscountType))
            throw new HttpException(400, "The selected discount type is invalid.");
        if (request.MaxQty.HasValue && request.MaxQty.Value < request.MinQty)
            throw new HttpException(400, "Maximum quantity must be greater than or equal to minimum quantity.");
        if (request.StartAtUtc.HasValue && request.EndAtUtc.HasValue && request.EndAtUtc <= request.StartAtUtc)
            throw new HttpException(400, "Promotion end time must be later than its start time.");
        if (request.EndAtUtc.HasValue && request.EndAtUtc <= dateTimeProvider.UtcNow)
            throw new HttpException(400, "Promotion end time must be in the future.");
        if (request.DiscountType == DiscountType.Percentage && request.DiscountValue > 100)
            throw new HttpException(400, "Percentage discounts cannot exceed 100 percent.");
        if (request.DiscountType == DiscountType.FixedPrice && !request.ProductId.HasValue)
            throw new HttpException(400, "Fixed-price discounts must target a specific product.");
        if (request.ProductId.HasValue && !await repository.ProductBelongsToShopAsync(request.ProductId.Value, shopId, cancellationToken))
            throw new HttpException(400, "The selected product does not belong to this shop.");
        if (await repository.NameExistsAsync(shopId, request.Name.Trim(), exceptPromotionId, cancellationToken))
            throw new HttpException(409, "A promotion with this name already exists in the shop.");
        if (await repository.HasOverlappingRangeAsync(shopId, request.ProductId, request.MinQty,
                request.MaxQty, request.StartAtUtc?.UtcDateTime, request.EndAtUtc?.UtcDateTime,
                exceptPromotionId, cancellationToken))
            throw new HttpException(409, B2BOrderingMessages.PromotionRangeOverlap);
    }

    private PromotionData BuildPromotion(Guid shopId, SavePromotionRequest request, Guid supplierUserId) => new(
        Guid.NewGuid(), shopId, request.ProductId, request.Name.Trim(), request.Description?.Trim(),
        request.MinQty, request.MaxQty, request.DiscountType, request.DiscountValue,
        request.StartAtUtc?.UtcDateTime, request.EndAtUtc?.UtcDateTime, true, supplierUserId, null,
        dateTimeProvider.UtcNow, dateTimeProvider.UtcNow);
}
