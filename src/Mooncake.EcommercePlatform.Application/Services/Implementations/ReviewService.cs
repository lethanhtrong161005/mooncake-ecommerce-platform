namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements review creation, reputation scoring adjustment, and audit log tracking.</summary>
public class ReviewService(
    IReviewRepository reviewRepository,
    IReputationLogRepository reputationLogRepository,
    ISupplierRepository supplierRepository,
    IReviewHelper reviewHelper) : IReviewService
{
    public async Task<ReviewResponse> CreateReviewAsync(CreateReviewRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Enforce DB constraint: either OrderId or ContractId, never both, never neither
        if (request.OrderId.HasValue && request.ContractId.HasValue)
        {
            throw new HttpException(400, "Review must target either an Order OR a Contract, not both.");
        }

        if (!request.OrderId.HasValue && !request.ContractId.HasValue)
        {
            throw new HttpException(400, "Review must specify either OrderId or ContractId.");
        }

        // 2. Prevent duplicate reviews (unique partial indexes in DB)
        if (request.OrderId.HasValue)
        {
            var existing = await reviewRepository.GetByOrderIdAsync(request.OrderId.Value, cancellationToken);
            if (existing != null)
            {
                throw new HttpException(400, "A review has already been submitted for this order.");
            }
        }

        if (request.ContractId.HasValue)
        {
            var existing = await reviewRepository.GetByContractIdAsync(request.ContractId.Value, cancellationToken);
            if (existing != null)
            {
                throw new HttpException(400, "A review has already been submitted for this contract.");
            }
        }

        var supplier = await supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken)
            ?? throw new HttpException(404, $"Supplier with ID {request.SupplierId} was not found.");

        var now = DateTime.UtcNow;

        // 3. Create review entity
        var review = new Review
        {
            CustomerId = request.CustomerId,
            SupplierId = request.SupplierId,
            OrderId = request.OrderId,
            ContractId = request.ContractId,
            Rating = request.Rating,
            Comment = request.Comment?.Trim(),
            CreatedAtUtc = now
        };

        var created = await reviewRepository.CreateAsync(review, cancellationToken);

        // 4. Calculate reputation score delta based on star rating
        var scoreDelta = request.Rating switch
        {
            5 => 10,
            4 => 5,
            3 => 0,
            2 => -10,
            1 => -20,
            _ => 0
        };

        // 5. Create audit log entry
        var log = new ReputationLog
        {
            SupplierId = supplier.Id,
            EventType = ReputationEventType.Review,
            ScoreDelta = scoreDelta,
            ReviewId = created.Id,
            ContractId = request.ContractId,
            Reason = $"Customer review rating {request.Rating}/5 stars",
            CreatedAtUtc = now
        };

        await reputationLogRepository.CreateAsync(log, cancellationToken);

        // 6. Update supplier's dynamic reputation score
        supplier.ReputationScore = Math.Clamp(supplier.ReputationScore + scoreDelta, 0, 1000);
        supplier.UpdatedAtUtc = now;
        await supplierRepository.UpdateAsync(supplier, cancellationToken);

        var loaded = await reviewRepository.GetByIdAsync(created.Id, cancellationToken);
        return reviewHelper.ToResponse(loaded ?? created);
    }

    public async Task<IEnumerable<ReviewResponse>> GetReviewsBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default)
    {
        var reviews = await reviewRepository.GetBySupplierIdAsync(supplierId, cancellationToken);
        return reviews.Select(reviewHelper.ToResponse);
    }

    public async Task<ReviewResponse?> GetReviewByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var review = await reviewRepository.GetByIdAsync(id, cancellationToken);
        return review == null ? null : reviewHelper.ToResponse(review);
    }

    public async Task<IEnumerable<ReputationLogResponse>> GetReputationLogsBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default)
    {
        var logs = await reputationLogRepository.GetBySupplierIdAsync(supplierId, cancellationToken);
        return logs.Select(reviewHelper.ToLogResponse);
    }
}
