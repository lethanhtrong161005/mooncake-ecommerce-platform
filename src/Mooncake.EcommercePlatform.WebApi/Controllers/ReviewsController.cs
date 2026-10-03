namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Customer reviews, supplier reputation scoring, and audit log endpoints.</summary>
[Route("api/v1/reviews")]
public class ReviewsController(IReviewService reviewService) : BaseApiController
{
    /// <summary>Submits a review for an Order or Contract, updating the supplier's dynamic reputation score.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateReviewAsync([FromBody] CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var result = await reviewService.CreateReviewAsync(request, cancellationToken);
        return Created(result, "Review submitted and reputation score updated successfully.");
    }

    /// <summary>Lists all customer reviews for a given supplier.</summary>
    [HttpGet("supplier/{supplierId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReviewsBySupplierIdAsync(long supplierId, CancellationToken cancellationToken)
    {
        var reviews = await reviewService.GetReviewsBySupplierIdAsync(supplierId, cancellationToken);
        return Success(reviews, "Supplier reviews retrieved successfully.");
    }

    /// <summary>Gets single review details by ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReviewByIdAsync(long id, CancellationToken cancellationToken)
    {
        var review = await reviewService.GetReviewByIdAsync(id, cancellationToken);
        if (review == null)
        {
            return NotFound($"Review with ID {id} was not found.");
        }

        return Success(review, "Review retrieved successfully.");
    }

    /// <summary>Gets chronological reputation audit logs for a supplier.</summary>
    [HttpGet("supplier/{supplierId:long}/reputation-logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReputationLogsBySupplierIdAsync(long supplierId, CancellationToken cancellationToken)
    {
        var logs = await reviewService.GetReputationLogsBySupplierIdAsync(supplierId, cancellationToken);
        return Success(logs, "Reputation logs retrieved successfully.");
    }
}
