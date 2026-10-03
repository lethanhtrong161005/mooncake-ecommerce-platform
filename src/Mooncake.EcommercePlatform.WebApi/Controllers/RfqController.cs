namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Rfq.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>B2B RFQ, Quotation submission, Price Negotiation, and Quotation acceptance endpoints.</summary>
[Route("api/v1/rfq")]
public class RfqController(IRfqService rfqService) : BaseApiController
{
    /// <summary>Creates a new B2B Request For Quotation (RFQ).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRfqAsync([FromBody] CreateRfqRequest request, CancellationToken cancellationToken)
    {
        var rfq = await rfqService.CreateRfqAsync(request, cancellationToken);
        return Created(rfq, "RFQ created successfully.");
    }

    /// <summary>Lists RFQs with optional customer and status filters.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRfqsAsync([FromQuery] long? customerId, [FromQuery] RfqStatus? status, CancellationToken cancellationToken)
    {
        var rfqs = await rfqService.GetRfqsAsync(customerId, status, cancellationToken);
        return Success(rfqs, "RFQs retrieved successfully.");
    }

    /// <summary>Gets single RFQ detail including requested items and received quotations count.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRfqByIdAsync(long id, CancellationToken cancellationToken)
    {
        var rfq = await rfqService.GetRfqByIdAsync(id, cancellationToken);
        if (rfq == null)
        {
            return NotFound($"RFQ with ID {id} was not found.");
        }

        return Success(rfq, "RFQ details retrieved successfully.");
    }

    /// <summary>Invites a supplier to participate and quote on an RFQ.</summary>
    [HttpPost("{id:long}/invite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InviteSupplierAsync(long id, [FromBody] InviteSupplierRequest request, CancellationToken cancellationToken)
    {
        await rfqService.InviteSupplierAsync(id, request, cancellationToken);
        return Success(true, "Supplier invited successfully.");
    }

    /// <summary>Gets RFQ invitations received by a specific supplier.</summary>
    [HttpGet("supplier/{supplierId:long}/invitations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSupplierInvitationsAsync(long supplierId, CancellationToken cancellationToken)
    {
        var invitations = await rfqService.GetSupplierInvitationsAsync(supplierId, cancellationToken);
        return Success(invitations, "Supplier invitations retrieved successfully.");
    }

    /// <summary>Submits a supplier's quotation with unit prices and lead time.</summary>
    [HttpPost("quotations")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitQuotationAsync([FromBody] SubmitQuotationRequest request, CancellationToken cancellationToken)
    {
        var quotation = await rfqService.SubmitQuotationAsync(request, cancellationToken);
        return Created(quotation, "Quotation submitted successfully.");
    }

    /// <summary>Lists all quotations submitted for a specific RFQ.</summary>
    [HttpGet("{id:long}/quotations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuotationsByRfqIdAsync(long id, CancellationToken cancellationToken)
    {
        var quotations = await rfqService.GetQuotationsByRfqIdAsync(id, cancellationToken);
        return Success(quotations, "Quotations retrieved successfully.");
    }

    /// <summary>Gets single quotation details by ID.</summary>
    [HttpGet("quotations/{quotationId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuotationByIdAsync(long quotationId, CancellationToken cancellationToken)
    {
        var quotation = await rfqService.GetQuotationByIdAsync(quotationId, cancellationToken);
        if (quotation == null)
        {
            return NotFound($"Quotation with ID {quotationId} was not found.");
        }

        return Success(quotation, "Quotation retrieved successfully.");
    }

    /// <summary>Gets all quotations submitted by a specific supplier.</summary>
    [HttpGet("supplier/{supplierId:long}/quotations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuotationsBySupplierIdAsync(long supplierId, CancellationToken cancellationToken)
    {
        var quotations = await rfqService.GetQuotationsBySupplierIdAsync(supplierId, cancellationToken);
        return Success(quotations, "Supplier quotations retrieved successfully.");
    }

    /// <summary>Initiates a new price negotiation round on a quotation.</summary>
    [HttpPost("quotations/{quotationId:long}/negotiate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateNegotiationAsync(long quotationId, [FromBody] CreateNegotiationRequest request, CancellationToken cancellationToken)
    {
        var result = await rfqService.CreateNegotiationAsync(quotationId, request, cancellationToken);
        return Success(result, "Negotiation round proposed successfully.");
    }

    /// <summary>Accepts or rejects an active price negotiation round.</summary>
    [HttpPost("negotiations/{negotiationId:long}/respond")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RespondNegotiationAsync(long negotiationId, [FromBody] RespondNegotiationRequest request, CancellationToken cancellationToken)
    {
        var result = await rfqService.RespondNegotiationAsync(negotiationId, request, cancellationToken);
        return Success(result, "Negotiation round responded successfully.");
    }

    /// <summary>Customer accepts a quotation, rejecting rivals, awarding RFQ, and generating a Contract with deposit &amp; final milestones.</summary>
    [HttpPost("quotations/{quotationId:long}/accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptQuotationAsync(long quotationId, CancellationToken cancellationToken)
    {
        var result = await rfqService.AcceptQuotationAsync(quotationId, cancellationToken);
        return Success(result, "Quotation accepted and Contract generated successfully.");
    }

    /// <summary>Returns side-by-side comparison of all quotations submitted for an RFQ.</summary>
    [HttpGet("{id:long}/compare")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareQuotationsAsync(long id, CancellationToken cancellationToken)
    {
        var comparison = await rfqService.CompareQuotationsAsync(id, cancellationToken);
        return Success(comparison, "Quotation comparison calculated successfully.");
    }

    /// <summary>Triggers background expiration check on open RFQs and submitted quotations.</summary>
    [HttpPost("check-expirations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckExpirationsAsync(CancellationToken cancellationToken)
    {
        await rfqService.CheckExpirationsAsync(cancellationToken);
        return Success(true, "Expirations checked and updated successfully.");
    }
}
