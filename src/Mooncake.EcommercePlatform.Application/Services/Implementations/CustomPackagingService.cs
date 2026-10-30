namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Packaging.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Packaging.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements the multi-round custom packaging approval workflow.</summary>
public sealed class CustomPackagingService(
    ICustomPackagingRepository repository,
    IDateTimeProvider dateTimeProvider) : ICustomPackagingService
{
    public async Task<CustomPackagingResponse> CreateAsync(Guid customerId, Guid orderId,
        CreateCustomPackagingRequest request, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderAsync(orderId, cancellationToken);
        if (order.CustomerId != customerId)
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound);
        if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
            throw new HttpException(409, B2BOrderingMessages.PackagingOrderStatusInvalid);
        if (await repository.GetByOrderIdAsync(orderId, cancellationToken) is not null)
            throw new HttpException(409, B2BOrderingMessages.PackagingAlreadyExists);

        var now = dateTimeProvider.UtcNow;
        var packaging = new CustomPackaging
        {
            OrderId = orderId,
            CompanyName = request.CompanyName.Trim(),
            CompanyLogoUrl = CustomPackagingRequestHelper.NormalizeHttpsUrl(request.CompanyLogoUrl),
            PackagingType = request.PackagingType.Trim(),
            ColorScheme = CustomPackagingRequestHelper.NormalizeColorScheme(request.ColorScheme),
            SpecialMessage = request.SpecialMessage?.Trim(),
            Notes = request.Notes?.Trim(),
            Status = CustomPackagingStatus.Pending,
            CreatedBy = customerId,
            CreatedAt = now,
            UpdatedAt = now
        };
        if (!await repository.CreateAsync(packaging, customerId, cancellationToken))
            throw new HttpException(409, B2BOrderingMessages.PackagingAlreadyExists);
        return CustomPackagingResponseHelper.ToResponse(packaging, []);
    }

    public async Task<CustomPackagingResponse> GetAsync(Guid userId, bool supplier, Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await GetOrderAsync(orderId, cancellationToken);
        var authorized = supplier
            ? await repository.IsShopOwnedByUserAsync(order.ShopId, userId, cancellationToken)
            : order.CustomerId == userId;
        if (!authorized)
            throw new HttpException(404, B2BOrderingMessages.PackagingNotFound);
        var packaging = await repository.GetByOrderIdAsync(orderId, cancellationToken)
                        ?? throw new HttpException(404, B2BOrderingMessages.PackagingNotFound);
        return CustomPackagingResponseHelper.ToResponse(packaging,
            await repository.GetRevisionsAsync(packaging.Id, cancellationToken));
    }

    public async Task<CustomPackagingResponse> SubmitRevisionAsync(Guid supplierUserId,
        Guid orderId, SubmitPackagingRevisionRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await GetOrderAsync(orderId, cancellationToken);
        if (!await repository.IsShopOwnedByUserAsync(order.ShopId, supplierUserId, cancellationToken))
            throw new HttpException(404, B2BOrderingMessages.PackagingNotFound);
        var packaging = await repository.GetByOrderIdAsync(orderId, cancellationToken)
                        ?? throw new HttpException(404, B2BOrderingMessages.PackagingNotFound);
        if (packaging.Status is not (CustomPackagingStatus.Pending or
            CustomPackagingStatus.DesignReview or CustomPackagingStatus.RevisionRequested))
            throw new HttpException(409, B2BOrderingMessages.PackagingRevisionLocked);

        var now = dateTimeProvider.UtcNow;
        var revision = new CustomPackagingRevision
        {
            CustomPackagingId = packaging.Id,
            MockupUrl = CustomPackagingRequestHelper.NormalizeHttpsUrl(request.MockupUrl),
            SupplierNotes = request.SupplierNotes?.Trim(),
            DesignFee = request.DesignFee,
            Status = CustomPackagingStatus.DesignReview,
            SubmittedByUserId = supplierUserId,
            SubmittedAt = now,
            CreatedBy = supplierUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
        var savedRevision = await repository.SubmitRevisionAsync(packaging, revision,
            order.CustomerId, cancellationToken)
            ?? throw new HttpException(409, B2BOrderingMessages.PackagingRevisionLocked);
        var revisions = await repository.GetRevisionsAsync(packaging.Id, cancellationToken);
        return CustomPackagingResponseHelper.ToResponse(packaging, revisions);
    }

    public async Task<PackagingReviewResponse> ReviewRevisionAsync(Guid customerId,
        Guid orderId, Guid revisionId, ReviewPackagingRevisionRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await GetOrderAsync(orderId, cancellationToken);
        if (order.CustomerId != customerId)
            throw new HttpException(404, B2BOrderingMessages.PackagingRevisionNotFound);
        var packaging = await repository.GetByOrderIdAsync(orderId, cancellationToken)
                        ?? throw new HttpException(404, B2BOrderingMessages.PackagingNotFound);
        var revision = await repository.GetRevisionAsync(revisionId, cancellationToken);
        if (revision is null || revision.CustomPackagingId != packaging.Id)
            throw new HttpException(404, B2BOrderingMessages.PackagingRevisionNotFound);
        if (revision.Status != CustomPackagingStatus.DesignReview)
            throw new HttpException(409, B2BOrderingMessages.PackagingRevisionStatusInvalid);

        var approve = string.Equals(request.Action, "Approve", StringComparison.Ordinal);
        var notes = request.Notes?.Trim();
        if (!approve && (notes?.Length ?? 0) < 10)
            throw new HttpException(400, B2BOrderingMessages.PackagingRevisionNotesRequired);
        var now = dateTimeProvider.UtcNow;
        if (!await repository.ReviewRevisionAsync(packaging, revision, approve, notes,
            customerId, now, cancellationToken))
            throw new HttpException(409, B2BOrderingMessages.PackagingRevisionStatusInvalid);
        return new PackagingReviewResponse(packaging.Status, packaging.BoxDesignUrl, now);
    }

    private async Task<Order> GetOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        await repository.GetOrderAsync(orderId, cancellationToken)
        ?? throw new HttpException(404, B2BOrderingMessages.OrderNotFound);
}
