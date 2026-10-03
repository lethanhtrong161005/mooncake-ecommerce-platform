namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Review and reputation mapping helper interface.</summary>
public interface IReviewHelper
{
    ReviewResponse ToResponse(Review review);
    ReputationLogResponse ToLogResponse(ReputationLog log);
}
