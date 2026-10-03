namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Rfq.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Rfq.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements B2B RFQ, Quotation, and Negotiation operations.</summary>
public class RfqService(
    IRfqRepository rfqRepository,
    IContractRepository contractRepository,
    IRfqHelper rfqHelper) : IRfqService
{
    public async Task<RfqResponse> CreateRfqAsync(CreateRfqRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var rfq = new RequestForQuotation
        {
            CustomerId = request.CustomerId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Visibility = request.Visibility,
            Status = RfqStatus.Open,
            QuoteDeadline = request.QuoteDeadline,
            RequiredDeliveryDate = request.RequiredDeliveryDate,
            DeliveryAddress = request.DeliveryAddress.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            RfqItems = request.Items.Select(i => new RfqItem
            {
                ProductId = i.ProductId,
                ItemName = i.ItemName.Trim(),
                Specification = i.Specification?.Trim(),
                Quantity = i.Quantity,
                CustomPackagingId = i.CustomPackagingId,
                Note = i.Note?.Trim()
            }).ToList()
        };

        if (request.InvitedSupplierIds is { Count: > 0 })
        {
            rfq.RfqInvitations = request.InvitedSupplierIds.Distinct().Select(supplierId => new RfqInvitation
            {
                SupplierId = supplierId,
                Status = RfqInvitationStatus.Invited,
                InvitedAtUtc = now
            }).ToList();
        }

        var created = await rfqRepository.CreateRfqAsync(rfq, cancellationToken);
        var loaded = await rfqRepository.GetByIdAsync(created.Id, cancellationToken);

        return rfqHelper.ToResponse(loaded ?? created);
    }

    public async Task<IEnumerable<RfqResponse>> GetRfqsAsync(long? customerId = null, RfqStatus? status = null, CancellationToken cancellationToken = default)
    {
        var rfqs = await rfqRepository.GetRfqsAsync(customerId, status, cancellationToken);
        return rfqs.Select(rfqHelper.ToResponse);
    }

    public async Task<RfqResponse?> GetRfqByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var rfq = await rfqRepository.GetByIdAsync(id, cancellationToken);
        return rfq == null ? null : rfqHelper.ToResponse(rfq);
    }

    public async Task<bool> InviteSupplierAsync(long rfqId, InviteSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var rfq = await rfqRepository.GetByIdAsync(rfqId, cancellationToken)
            ?? throw new HttpException(404, $"RFQ with ID {rfqId} was not found.");

        var invitation = new RfqInvitation
        {
            RfqId = rfq.Id,
            SupplierId = request.SupplierId,
            Status = RfqInvitationStatus.Invited,
            InvitedAtUtc = DateTime.UtcNow
        };

        await rfqRepository.AddInvitationAsync(invitation, cancellationToken);
        return true;
    }

    public async Task<IEnumerable<RfqResponse>> GetSupplierInvitationsAsync(long supplierId, CancellationToken cancellationToken = default)
    {
        var invitations = await rfqRepository.GetInvitationsBySupplierIdAsync(supplierId, cancellationToken);
        return invitations
            .Where(i => i.RequestForQuotation != null)
            .Select(i => rfqHelper.ToResponse(i.RequestForQuotation!));
    }

    public async Task<QuotationResponse> SubmitQuotationAsync(SubmitQuotationRequest request, CancellationToken cancellationToken = default)
    {
        var rfq = await rfqRepository.GetByIdAsync(request.RfqId, cancellationToken)
            ?? throw new HttpException(404, $"RFQ with ID {request.RfqId} was not found.");

        if (rfq.Status != RfqStatus.Open)
        {
            throw new HttpException(400, "RFQ is not open for quotation submissions.");
        }

        if (rfq.QuoteDeadline.HasValue && rfq.QuoteDeadline.Value < DateTime.UtcNow)
        {
            throw new HttpException(400, "RFQ quote deadline has passed.");
        }

        // Validate items against RFQ items
        var rfqItemMap = rfq.RfqItems.ToDictionary(i => i.Id);
        var now = DateTime.UtcNow;

        var quotationItems = new List<QuotationItem>();
        decimal totalAmount = 0m;

        foreach (var reqItem in request.Items)
        {
            if (!rfqItemMap.TryGetValue(reqItem.RfqItemId, out var rfqItem))
            {
                throw new HttpException(400, $"Item ID {reqItem.RfqItemId} does not belong to this RFQ.");
            }

            var lineTotal = rfqItem.Quantity * reqItem.UnitPrice;
            totalAmount += lineTotal;

            quotationItems.Add(new QuotationItem
            {
                RfqItemId = reqItem.RfqItemId,
                Quantity = rfqItem.Quantity,
                UnitPrice = reqItem.UnitPrice,
                LineTotal = lineTotal,
                Note = reqItem.Note?.Trim()
            });
        }

        var quotation = new Quotation
        {
            RfqId = request.RfqId,
            SupplierId = request.SupplierId,
            Status = QuotationStatus.Submitted,
            TotalAmount = totalAmount,
            LeadTimeDays = request.LeadTimeDays,
            ProposedDepositPercent = request.ProposedDepositPercent,
            ValidUntilUtc = request.ValidUntilUtc ?? now.AddDays(14),
            Notes = request.Notes?.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            QuotationItems = quotationItems
        };

        var created = await rfqRepository.CreateQuotationAsync(quotation, cancellationToken);
        var loaded = await rfqRepository.GetQuotationByIdAsync(created.Id, cancellationToken);

        return rfqHelper.ToResponse(loaded ?? created);
    }

    public async Task<IEnumerable<QuotationResponse>> GetQuotationsByRfqIdAsync(long rfqId, CancellationToken cancellationToken = default)
    {
        var quotations = await rfqRepository.GetQuotationsByRfqIdAsync(rfqId, cancellationToken);
        return quotations.Select(rfqHelper.ToResponse);
    }

    public async Task<IEnumerable<QuotationResponse>> GetQuotationsBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default)
    {
        var quotations = await rfqRepository.GetQuotationsBySupplierIdAsync(supplierId, cancellationToken);
        return quotations.Select(rfqHelper.ToResponse);
    }

    public async Task<QuotationResponse?> GetQuotationByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var quotation = await rfqRepository.GetQuotationByIdAsync(id, cancellationToken);
        return quotation == null ? null : rfqHelper.ToResponse(quotation);
    }

    public async Task<PriceNegotiationResponse> CreateNegotiationAsync(long quotationId, CreateNegotiationRequest request, CancellationToken cancellationToken = default)
    {
        var quotation = await rfqRepository.GetQuotationByIdAsync(quotationId, cancellationToken)
            ?? throw new HttpException(404, $"Quotation with ID {quotationId} was not found.");

        if (quotation.Status != QuotationStatus.Submitted && quotation.Status != QuotationStatus.Negotiating)
        {
            throw new HttpException(400, "Cannot negotiate on a quotation that is not active or already finalized.");
        }

        var roundNo = (short)((quotation.PriceNegotiations?.Count ?? 0) + 1);
        var now = DateTime.UtcNow;

        // Supersede previous pending negotiations
        if (quotation.PriceNegotiations != null)
        {
            foreach (var existing in quotation.PriceNegotiations.Where(n => n.Status == NegotiationStatus.Pending))
            {
                existing.Status = NegotiationStatus.Superseded;
                await rfqRepository.UpdateNegotiationAsync(existing, cancellationToken);
            }
        }

        var negotiation = new PriceNegotiation
        {
            QuotationId = quotationId,
            RoundNo = roundNo,
            ProposedBy = request.ProposedBy,
            ProposedAmount = request.ProposedAmount,
            Message = request.Message?.Trim(),
            Status = NegotiationStatus.Pending,
            CreatedAtUtc = now
        };

        quotation.Status = QuotationStatus.Negotiating;
        quotation.UpdatedAtUtc = now;
        await rfqRepository.UpdateQuotationAsync(quotation, cancellationToken);

        var created = await rfqRepository.AddNegotiationAsync(negotiation, cancellationToken);

        return new PriceNegotiationResponse(
            created.Id,
            created.QuotationId,
            created.RoundNo,
            created.ProposedBy,
            created.ProposedAmount,
            created.Message,
            created.Status,
            created.CreatedAtUtc
        );
    }

    public async Task<PriceNegotiationResponse> RespondNegotiationAsync(long negotiationId, RespondNegotiationRequest request, CancellationToken cancellationToken = default)
    {
        var negotiation = await rfqRepository.GetNegotiationByIdAsync(negotiationId, cancellationToken)
            ?? throw new HttpException(404, $"Negotiation with ID {negotiationId} was not found.");

        if (negotiation.Status != NegotiationStatus.Pending)
        {
            throw new HttpException(400, "Negotiation round has already been resolved.");
        }

        var quotation = await rfqRepository.GetQuotationByIdAsync(negotiation.QuotationId, cancellationToken)
            ?? throw new HttpException(404, "Associated quotation was not found.");

        var now = DateTime.UtcNow;

        if (request.Accept)
        {
            negotiation.Status = NegotiationStatus.Accepted;
            quotation.TotalAmount = negotiation.ProposedAmount;
            quotation.UpdatedAtUtc = now;
            await rfqRepository.UpdateQuotationAsync(quotation, cancellationToken);
        }
        else
        {
            negotiation.Status = NegotiationStatus.Rejected;
        }

        var updated = await rfqRepository.UpdateNegotiationAsync(negotiation, cancellationToken);

        return new PriceNegotiationResponse(
            updated.Id,
            updated.QuotationId,
            updated.RoundNo,
            updated.ProposedBy,
            updated.ProposedAmount,
            updated.Message,
            updated.Status,
            updated.CreatedAtUtc
        );
    }

    public async Task<QuotationResponse> AcceptQuotationAsync(long quotationId, CancellationToken cancellationToken = default)
    {
        var quotation = await rfqRepository.GetQuotationByIdAsync(quotationId, cancellationToken)
            ?? throw new HttpException(404, $"Quotation with ID {quotationId} was not found.");

        if (quotation.Status != QuotationStatus.Submitted && quotation.Status != QuotationStatus.Negotiating)
        {
            throw new HttpException(400, "Quotation is not in an acceptable state.");
        }

        var rfq = quotation.RequestForQuotation
            ?? await rfqRepository.GetByIdAsync(quotation.RfqId, cancellationToken)
            ?? throw new HttpException(404, "Associated RFQ was not found.");

        var now = DateTime.UtcNow;

        // 1. Mark accepted quotation
        quotation.Status = QuotationStatus.Accepted;
        quotation.UpdatedAtUtc = now;
        await rfqRepository.UpdateQuotationAsync(quotation, cancellationToken);

        // 2. Reject other quotations for this RFQ
        var otherQuotations = await rfqRepository.GetQuotationsByRfqIdAsync(rfq.Id, cancellationToken);
        foreach (var other in otherQuotations.Where(q => q.Id != quotationId && q.Status != QuotationStatus.Rejected))
        {
            other.Status = QuotationStatus.Rejected;
            other.UpdatedAtUtc = now;
            await rfqRepository.UpdateQuotationAsync(other, cancellationToken);
        }

        // 3. Mark RFQ awarded
        rfq.Status = RfqStatus.Awarded;
        rfq.UpdatedAtUtc = now;
        await rfqRepository.UpdateRfqAsync(rfq, cancellationToken);

        // 4. Automatically create Contract + ContractMilestones
        var depositAmount = Math.Round(quotation.TotalAmount * (quotation.ProposedDepositPercent / 100m), 2);
        var finalAmount = quotation.TotalAmount - depositAmount;

        var deadline = rfq.RequiredDeliveryDate
            ?? DateOnly.FromDateTime(now.AddDays(quotation.LeadTimeDays ?? 14));

        var contract = new Contract
        {
            QuotationId = quotation.Id,
            CustomerId = rfq.CustomerId,
            SupplierId = quotation.SupplierId,
            Status = ContractStatus.Draft,
            TotalAmount = quotation.TotalAmount,
            DepositPercent = quotation.ProposedDepositPercent,
            DeliveryDeadline = deadline,
            LatePenaltyPercentPerDay = 0.5m,
            MaxPenaltyPercent = 10.0m,
            Terms = "Standard B2B Mooncake Production & Supply Agreement",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ContractMilestones =
            [
                new ContractMilestone
                {
                    MilestoneNo = 1,
                    Name = "Deposit Payment",
                    MilestoneType = MilestoneType.Deposit,
                    Amount = depositAmount,
                    DueDate = DateOnly.FromDateTime(now.AddDays(7)),
                    Status = MilestoneStatus.Pending,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                },
                new ContractMilestone
                {
                    MilestoneNo = 2,
                    Name = "Final Payment",
                    MilestoneType = MilestoneType.Final,
                    Amount = finalAmount,
                    DueDate = deadline,
                    Status = MilestoneStatus.Pending,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }
            ]
        };

        await contractRepository.CreateAsync(contract, cancellationToken);

        var loadedQuotation = await rfqRepository.GetQuotationByIdAsync(quotation.Id, cancellationToken);
        return rfqHelper.ToResponse(loadedQuotation ?? quotation);
    }

    public async Task<RfqComparisonResponse> CompareQuotationsAsync(long rfqId, CancellationToken cancellationToken = default)
    {
        var rfq = await rfqRepository.GetByIdAsync(rfqId, cancellationToken)
            ?? throw new HttpException(404, $"RFQ with ID {rfqId} was not found.");

        var quotations = await rfqRepository.GetQuotationsByRfqIdAsync(rfqId, cancellationToken);
        return rfqHelper.ToComparisonResponse(rfq, quotations);
    }

    public async Task CheckExpirationsAsync(CancellationToken cancellationToken = default)
    {
        await rfqRepository.CheckAndExpireRfqsAndQuotationsAsync(cancellationToken);
    }
}
