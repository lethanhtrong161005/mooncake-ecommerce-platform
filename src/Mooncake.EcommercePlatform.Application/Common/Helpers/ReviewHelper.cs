namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps Review and ReputationLog entities to response DTOs.</summary>
public class ReviewHelper : IReviewHelper
{
    public ReviewResponse ToResponse(Review review)
    {
        var customerName = review.Customer?.CompanyName
            ?? review.Customer?.User?.FullName
            ?? $"Customer #{review.CustomerId}";

        var supplierName = review.Supplier?.BusinessName
            ?? review.Supplier?.User?.FullName
            ?? $"Supplier #{review.SupplierId}";

        return new ReviewResponse(
            review.Id,
            review.CustomerId,
            customerName,
            review.SupplierId,
            supplierName,
            review.OrderId,
            review.ContractId,
            review.Rating,
            review.Comment,
            review.CreatedAtUtc
        );
    }

    public ReputationLogResponse ToLogResponse(ReputationLog log) =>
        new(
            log.Id,
            log.SupplierId,
            log.EventType,
            log.ScoreDelta,
            log.ReviewId,
            log.ContractId,
            log.Reason,
            log.CreatedAtUtc
        );
}
