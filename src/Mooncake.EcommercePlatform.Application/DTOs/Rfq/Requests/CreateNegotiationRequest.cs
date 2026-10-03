namespace Mooncake.EcommercePlatform.Application.DTOs.Rfq.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Request to initiate a new price negotiation round on a quotation.</summary>
public record CreateNegotiationRequest
{
    [Required]
    public NegotiationProposedBy ProposedBy { get; init; }

    [Required]
    [Range(1, 10000000000)]
    public decimal ProposedAmount { get; init; }

    public string? Message { get; init; }
}

/// <summary>Request to accept or reject a price negotiation round.</summary>
public record RespondNegotiationRequest
{
    [Required]
    public bool Accept { get; init; }

    public string? Note { get; init; }
}
