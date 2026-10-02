namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>B2B Contract management, dual signing, terms updates, penalty calculation, and completion.</summary>
[Route("api/v1/contracts")]
public class ContractsController(IContractService contractService) : BaseApiController
{
    /// <summary>Lists contracts with optional customer, supplier, and status filters.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContractsAsync(
        [FromQuery] long? customerId,
        [FromQuery] long? supplierId,
        [FromQuery] ContractStatus? status,
        CancellationToken cancellationToken)
    {
        var contracts = await contractService.GetContractsAsync(customerId, supplierId, status, cancellationToken);
        return Success(contracts, "Contracts retrieved successfully.");
    }

    /// <summary>Gets single contract details including milestones, terms, and penalty breakdown if overdue.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContractByIdAsync(long id, CancellationToken cancellationToken)
    {
        var contract = await contractService.GetContractByIdAsync(id, cancellationToken);
        if (contract == null)
        {
            return NotFound($"Contract with ID {id} was not found.");
        }

        return Success(contract, "Contract details retrieved successfully.");
    }

    /// <summary>Signs the contract digitally by Customer or Supplier. Activates contract once both sign.</summary>
    [HttpPost("{id:long}/sign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SignContractAsync(long id, [FromBody] SignContractRequest request, CancellationToken cancellationToken)
    {
        var contract = await contractService.SignContractAsync(id, request, cancellationToken);
        return Success(contract, "Contract signed successfully.");
    }

    /// <summary>Updates contract terms and penalty rates while contract is in Draft status.</summary>
    [HttpPut("{id:long}/terms")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTermsAsync(long id, [FromBody] UpdateContractTermsRequest request, CancellationToken cancellationToken)
    {
        var contract = await contractService.UpdateTermsAsync(id, request, cancellationToken);
        return Success(contract, "Contract terms updated successfully.");
    }

    /// <summary>Calculates late delivery penalty and net payout amount for a contract.</summary>
    [HttpGet("{id:long}/penalty")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPenaltyInfoAsync(long id, CancellationToken cancellationToken)
    {
        var penalty = await contractService.GetPenaltyInfoAsync(id, cancellationToken);
        return Success(penalty, "Penalty calculation retrieved successfully.");
    }

    /// <summary>Marks an active contract as Completed once all milestones have been paid.</summary>
    [HttpPost("{id:long}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteContractAsync(long id, CancellationToken cancellationToken)
    {
        var contract = await contractService.CompleteContractAsync(id, cancellationToken);
        return Success(contract, "Contract marked as Completed successfully.");
    }
}
