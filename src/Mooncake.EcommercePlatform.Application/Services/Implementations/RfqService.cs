namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Rfqs.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Rfqs.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed class RfqService(IRfqRepository repository, IDateTimeProvider clock, IContractService? contractService = null,
    IRealtimeNotificationPublisher? realtimePublisher = null) : IRfqService
{
    private const int MaximumInvitations = 10;

    public async Task<PagedResult<RfqResponse>> GetPageAsync(Guid userId, bool supplier,
        RfqPageQueryRequest query, CancellationToken cancellationToken = default)
    {
        PagedQueryHelper.Validate(query);
        PagedQueryHelper.ValidateSort(query.Sort, "createdAtDesc", "createdAtAsc", "deadlineAsc", "deadlineDesc");
        if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value))
            throw new HttpException(400, "Invalid RFQ status filter.", "VALIDATION_ERROR");
        if (supplier) _ = await GetVerifiedSupplierIdAsync(userId, cancellationToken);
        var page = await repository.GetRfqsPageAsync(userId, supplier, query, cancellationToken);
        if (page.Items.Count == 0)
            return new PagedResult<RfqResponse>([], page.Page, page.PageSize, page.TotalCount);
        var (items, invitations, bids) = await repository.GetRfqPageDetailsAsync(
            page.Items.Select(rfq => rfq.Id).ToArray(), cancellationToken);
        var byRfq = items.GroupBy(item => item.RfqId).ToDictionary(group => group.Key, group => group.ToList());
        var responses = page.Items.Select(rfq => new RfqResponse(rfq.Id, rfq.RfqNumber, rfq.Title,
            rfq.Description, rfq.DeliveryAddress, rfq.DeliveryDateRequired, rfq.BudgetRangeMin,
            rfq.BudgetRangeMax, rfq.BidDeadline, rfq.Status,
            byRfq.GetValueOrDefault(rfq.Id, []).Select(item => new RfqItemResponse(item.Id,
                item.ProductName, item.Description, item.Quantity, item.Unit,
                supplier ? null : item.TargetPrice, item.Specifications, item.PackagingRequirements,
                item.NeedsCustomPackaging)).ToArray(),
            invitations.GetValueOrDefault(rfq.Id), bids.GetValueOrDefault(rfq.Id))).ToArray();
        return new PagedResult<RfqResponse>(responses, page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<RfqResponse> CreateAsync(Guid customerUserId, CreateRfqRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var now = clock.UtcNow;
        var rfq = new RequestForQuotation
        {
            Id = Guid.NewGuid(),
            CustomerId = customerUserId,
            RfqNumber = $"RFQ-{now:yyyy}-{Guid.NewGuid():N}"[..19].ToUpperInvariant(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            DeliveryAddress = request.DeliveryAddress.Trim(),
            DeliveryDateRequired = request.DeliveryDateRequired,
            BudgetRangeMin = request.BudgetRangeMin,
            BudgetRangeMax = request.BudgetRangeMax,
            BidDeadline = DateTime.SpecifyKind(request.BidDeadline, DateTimeKind.Utc),
            Status = RfqStatus.Draft,
            CreatedBy = customerUserId
        };
        var items = request.Items.Select(item => new RfqItem
        {
            RfqId = rfq.Id,
            ProductName = item.ProductName.Trim(),
            Description = item.Description?.Trim(),
            Quantity = item.Quantity,
            Unit = item.Unit.Trim(),
            TargetPrice = item.TargetPrice,
            Specifications = item.Specifications?.Trim(),
            PackagingRequirements = item.PackagingRequirements?.Trim(),
            NeedsCustomPackaging = item.NeedsCustomPackaging,
            CreatedBy = customerUserId
        }).ToList();
        if (!await repository.CreateRfqAsync(rfq, items, cancellationToken))
            throw new HttpException(409, "The RFQ could not be created. Please retry the request.");
        return await ToResponseAsync(rfq, false, cancellationToken);
    }

    public async Task<RfqResponse> PublishAsync(Guid customerUserId, Guid rfqId, CancellationToken cancellationToken = default)
    {
        var rfq = await GetOwnedRfqAsync(customerUserId, rfqId, cancellationToken);
        if (rfq.Status != RfqStatus.Draft)
            throw new HttpException(409, "Only a draft RFQ can be published.");
        if (rfq.BidDeadline <= clock.UtcNow || rfq.DeliveryDateRequired < DateOnly.FromDateTime(clock.UtcNow))
            throw new HttpException(400, "The bid deadline and required delivery date must be in the future.");
        if (await repository.GetRfqItemsAsync(rfqId, cancellationToken) is not { Count: > 0 })
            throw new HttpException(400, "An RFQ must include at least one requested item.");
        rfq.Status = RfqStatus.Open;
        rfq.UpdatedBy = customerUserId;
        await repository.SaveRfqAsync(rfq, cancellationToken);
        return await ToResponseAsync(rfq, false, cancellationToken);
    }

    public async Task CancelAsync(Guid customerUserId, Guid rfqId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new HttpException(400, "A cancellation reason is required.");
        var rfq = await GetOwnedRfqAsync(customerUserId, rfqId, cancellationToken);
        if (rfq.Status is RfqStatus.Awarded or RfqStatus.Cancelled or RfqStatus.InNegotiation)
            throw new HttpException(409, "An awarded, pending-confirmation, or cancelled RFQ cannot be cancelled.");
        if (!await repository.CancelRfqAsync(rfqId, customerUserId, reason.Trim(), cancellationToken))
            throw new HttpException(409, "The RFQ changed before it could be cancelled.");
    }

    public async Task<IReadOnlyList<RfqResponse>> GetMineAsync(Guid customerUserId, CancellationToken cancellationToken = default)
    {
        var rfqs = await repository.GetCustomerRfqsAsync(customerUserId, cancellationToken);
        var responses = new List<RfqResponse>(rfqs.Count);
        foreach (var rfq in rfqs) responses.Add(await ToResponseAsync(rfq, false, cancellationToken));
        return responses;
    }

    public async Task<IReadOnlyList<RfqResponse>> GetSupplierInvitationsAsync(Guid supplierUserId,
        CancellationToken cancellationToken = default)
    {
        var supplierId = await GetVerifiedSupplierIdAsync(supplierUserId, cancellationToken);
        if (!await repository.IsActiveSupplierAsync(supplierId, cancellationToken))
            throw new HttpException(403, "An active supplier shop is required to view RFQs.");
        var rfqs = await repository.GetSupplierRfqsAsync(supplierUserId, cancellationToken);
        var responses = new List<RfqResponse>(rfqs.Count);
        foreach (var rfq in rfqs)
        {
            var invitation = await repository.GetInvitationAsync(rfq.Id, supplierId, cancellationToken);
            if (invitation?.Status == RfqInvitationStatus.Declined)
                continue;
            responses.Add(await ToResponseAsync(rfq, true, cancellationToken, invitation is not null,
                invitation?.Status is RfqInvitationStatus.Invited or RfqInvitationStatus.Viewed, supplierId));
        }
        return responses;
    }

    public async Task<IReadOnlyList<SuggestedSupplierResponse>> GetSuggestedSuppliersAsync(Guid customerUserId,
        Guid rfqId, CancellationToken cancellationToken = default)
    {
        var rfq = await GetOwnedRfqAsync(customerUserId, rfqId, cancellationToken);
        EnsureOpen(rfq);
        var suppliers = await repository.GetSuggestedSuppliersAsync(rfqId, cancellationToken);
        return suppliers.Select(profile => new SuggestedSupplierResponse(profile.UserId, profile.Id,
            profile.CompanyName ?? "Verified supplier", profile.ReputationScore, profile.AvgRating,
            profile.Address)).ToArray();
    }

    public async Task<RfqResponse> GetAsync(Guid userId, bool isSupplier, Guid rfqId, CancellationToken cancellationToken = default)
    {
        var rfq = await repository.GetRfqAsync(rfqId, cancellationToken);
        if (rfq is null || rfq.IsDeleted) throw new HttpException(404, "RFQ was not found.");
        if (rfq.CustomerId != userId)
        {
            var supplierId = isSupplier ? await repository.GetVerifiedSupplierProfileIdAsync(userId, cancellationToken) : null;
            if (!supplierId.HasValue || !await repository.IsActiveSupplierAsync(supplierId.Value, cancellationToken))
                throw new HttpException(404, "RFQ was not found.");
            var invitation = await repository.GetInvitationAsync(rfqId, supplierId.Value, cancellationToken);
            var isInvited = invitation is not null;
            if (!isInvited && (rfq.Status != RfqStatus.Open || rfq.BidDeadline <= clock.UtcNow))
                throw new HttpException(404, "RFQ was not found.");
            if (rfq.Status == RfqStatus.Open && rfq.BidDeadline > clock.UtcNow)
                if (invitation is not null)
                    await MarkViewedAsync(rfqId, supplierId.Value, cancellationToken);
            return await ToResponseAsync(rfq, true, cancellationToken, isInvited,
                invitation?.Status is RfqInvitationStatus.Invited or RfqInvitationStatus.Viewed, supplierId.Value);
        }
        return await ToResponseAsync(rfq, false, cancellationToken);
    }

    public async Task<RfqResponse> InviteAsync(Guid customerUserId, Guid rfqId, InviteSuppliersRequest request, CancellationToken cancellationToken = default)
    {
        var rfq = await GetOwnedRfqAsync(customerUserId, rfqId, cancellationToken);
        EnsureOpen(rfq);
        if (request.SupplierUserIds.Count == 0) throw new HttpException(400, "Select at least one supplier to invite.");
        var existing = await repository.GetInvitationsAsync(rfqId, cancellationToken);
        var userIds = request.SupplierUserIds.Distinct().ToArray();
        if (userIds.Length != request.SupplierUserIds.Count) throw new HttpException(400, "Supplier IDs must be unique.");
        if (existing.Count + userIds.Length > MaximumInvitations) throw new HttpException(409, "An RFQ can invite at most 10 suppliers.");

        var invited = new List<RfqInvitation>(userIds.Length);
        foreach (var supplierUserId in userIds)
        {
            var profileId = await repository.GetVerifiedSupplierProfileIdAsync(supplierUserId, cancellationToken);
            if (!profileId.HasValue || !await repository.IsActiveSupplierAsync(profileId.Value, cancellationToken))
                throw new HttpException(400, "Every invited supplier must be verified and have an active shop.");
            if (existing.Any(invitation => invitation.SupplierId == profileId.Value))
                throw new HttpException(409, "A supplier has already been invited to this RFQ.");
            invited.Add(new RfqInvitation { RfqId = rfqId, SupplierId = profileId.Value, InvitedAt = clock.UtcNow, CreatedBy = customerUserId });
        }
        await repository.AddInvitationsAsync(invited, cancellationToken);
        return await ToResponseAsync(rfq, false, cancellationToken);
    }

    public async Task DeclineAsync(Guid supplierUserId, Guid rfqId, DeclineInvitationRequest request, CancellationToken cancellationToken = default)
    {
        var profileId = await GetVerifiedSupplierIdAsync(supplierUserId, cancellationToken);
        var rfq = await repository.GetRfqAsync(rfqId, cancellationToken);
        if (rfq is null || rfq.IsDeleted) throw new HttpException(404, "RFQ was not found.");
        EnsureOpen(rfq);
        var invitation = await repository.GetInvitationAsync(rfqId, profileId, cancellationToken)
                         ?? throw new HttpException(404, "RFQ invitation was not found.");
        if (invitation.Status is not (RfqInvitationStatus.Invited or RfqInvitationStatus.Viewed))
            throw new HttpException(409, "This RFQ invitation has already been answered.");
        invitation.Status = RfqInvitationStatus.Declined;
        invitation.RespondedAt = clock.UtcNow;
        invitation.DeclineReason = request.Reason.Trim();
        invitation.UpdatedBy = supplierUserId;
        await repository.SaveInvitationAsync(invitation, cancellationToken);
    }

    public async Task<BidResponse> SubmitBidAsync(Guid supplierUserId, Guid rfqId, SubmitBidRequest request, CancellationToken cancellationToken = default)
    {
        var supplierId = await GetVerifiedSupplierIdAsync(supplierUserId, cancellationToken);
        if (!await repository.IsActiveSupplierAsync(supplierId, cancellationToken))
            throw new HttpException(403, "An active supplier shop is required to submit a bid.");
        var rfq = await repository.GetRfqAsync(rfqId, cancellationToken);
        if (rfq is null || rfq.IsDeleted) throw new HttpException(404, "RFQ was not found.");
        EnsureOpen(rfq);
        var invitation = await repository.GetInvitationAsync(rfqId, supplierId, cancellationToken);
        if (invitation?.Status == RfqInvitationStatus.Declined)
            throw new HttpException(409, "This RFQ invitation has already been answered.");
        var rfqItems = await repository.GetRfqItemsAsync(rfqId, cancellationToken);
        if (request.Items.Count == 0 || request.Items.Any(item => item.UnitPrice <= 0)
            || request.Items.Count != rfqItems.Count || request.Items.Select(item => item.RfqItemId).Distinct().Count() != request.Items.Count
            || request.Items.Any(item => rfqItems.All(rfqItem => rfqItem.Id != item.RfqItemId)))
            throw new HttpException(400, "A bid must include a price for every RFQ item exactly once.");
        var rfqItemMap = rfqItems.ToDictionary(item => item.Id);
        var now = clock.UtcNow;
        var existingBid = (await repository.GetBidsAsync(rfqId, supplierId, cancellationToken))
            .SingleOrDefault(existing => existing.Status == BidStatus.Submitted);
        var bid = new Bid
        {
            Id = existingBid?.Id ?? Guid.NewGuid(),
            RfqId = rfqId,
            SupplierId = supplierId,
            BidNumber = existingBid?.BidNumber ?? $"BID-{now:yyyy}-{Guid.NewGuid():N}"[..19].ToUpperInvariant(),
            EstimatedDeliveryDays = request.EstimatedDeliveryDays,
            ValidityDays = request.ValidityDays,
            Notes = request.Notes?.Trim(),
            SubmittedAt = now,
            Status = BidStatus.Submitted,
            CreatedBy = supplierUserId
        };
        var items = request.Items.Select(item => new BidItem
        {
            BidId = bid.Id,
            RfqItemId = item.RfqItemId,
            UnitPrice = item.UnitPrice,
            Quantity = rfqItemMap[item.RfqItemId].Quantity,
            Subtotal = decimal.Round(item.UnitPrice * rfqItemMap[item.RfqItemId].Quantity, 2, MidpointRounding.AwayFromZero),
            Notes = item.Notes?.Trim(),
            CreatedBy = supplierUserId
        }).ToList();
        bid.TotalPrice = items.Sum(item => item.Subtotal);
        if (!await repository.CreateBidAsync(bid, items, cancellationToken))
            throw new HttpException(409, "The supplier or shop is no longer eligible to submit a bid for this RFQ.");
        if (realtimePublisher is not null)
            await realtimePublisher.PublishToUserAsync(rfq.CustomerId, "MarketplaceChanged",
                new { category = "rfq", referenceId = rfqId, type = "bid.submitted" }, cancellationToken);
        return await ToBidResponseAsync(bid, cancellationToken);
    }

    public async Task<IReadOnlyList<BidResponse>> CompareBidsAsync(Guid customerUserId, Guid rfqId, CancellationToken cancellationToken = default)
    {
        _ = await GetOwnedRfqAsync(customerUserId, rfqId, cancellationToken);
        var bids = await repository.GetBidsAsync(rfqId, cancellationToken: cancellationToken);
        var responses = new List<BidResponse>(bids.Count);
        foreach (var bid in bids) responses.Add(await ToBidResponseAsync(bid, cancellationToken));
        return responses;
    }

    public async Task<BidResponse> CancelBidAsync(Guid customerUserId, Guid rfqId, Guid bidId, CancellationToken cancellationToken = default)
    {
        var rfq = await GetOwnedRfqAsync(customerUserId, rfqId, cancellationToken);
        if (rfq.Status is RfqStatus.Awarded or RfqStatus.Cancelled) throw new HttpException(409, "This RFQ no longer accepts bid changes.");
        var bid = await repository.GetBidAsync(bidId, cancellationToken);
        if (bid is null || bid.RfqId != rfqId) throw new HttpException(404, "Bid was not found.");
        if (!await repository.CancelBidAsync(rfqId, bidId, customerUserId, cancellationToken))
            throw new HttpException(409, "Only a submitted bid can be cancelled.");
        bid.Status = BidStatus.Cancelled;
        return await ToBidResponseAsync(bid, cancellationToken);
    }

    public async Task<BidSelectionResponse> AwardBidAsync(Guid customerUserId, Guid rfqId, Guid bidId, AwardBidRequest request, CancellationToken cancellationToken = default)
    {
        var rfq = await GetOwnedRfqAsync(customerUserId, rfqId, cancellationToken);
        if (rfq.Status == RfqStatus.InNegotiation)
        {
            if (rfq.AwardedBidId == bidId)
                return new(true, null, "Đang chờ supplier xác nhận lựa chọn.");
            throw new HttpException(409, "RFQ đang chờ supplier khác xác nhận.");
        }
        if (rfq.Status is not (RfqStatus.Open or RfqStatus.Closed)) throw new HttpException(409, "RFQ không còn nhận lựa chọn báo giá.");
        var selected = await repository.GetBidAsync(bidId, cancellationToken);
        if (selected is null || selected.RfqId != rfqId || selected.Status != BidStatus.Submitted)
            throw new HttpException(404, "A submitted bid for this RFQ was not found.");
        if (selected.SubmittedAt.AddDays(selected.ValidityDays) <= clock.UtcNow)
            throw new HttpException(409, "This bid has expired.");
        if (!await repository.RequestBidSelectionAsync(rfqId, bidId, request.SelectionNote?.Trim(), customerUserId, cancellationToken))
            throw new HttpException(409, "Không thể gửi yêu cầu chọn supplier. Vui lòng tải lại RFQ.");
        return new(true, null, "Đã gửi yêu cầu chọn supplier. RFQ tạm đóng trong khi chờ supplier xác nhận.");
    }

    public async Task<BidSelectionResponse> RespondToBidSelectionAsync(Guid supplierUserId, Guid rfqId, Guid bidId, bool accept, CancellationToken cancellationToken = default)
    {
        var supplierProfileId = await GetVerifiedSupplierIdAsync(supplierUserId, cancellationToken);
        if (!await repository.IsActiveSupplierAsync(supplierProfileId, cancellationToken))
            throw new HttpException(403, "An active supplier shop is required to respond to a bid selection.");
        var bid = await repository.GetBidAsync(bidId, cancellationToken);
        if (bid is null || bid.RfqId != rfqId || bid.SupplierId != supplierProfileId)
            throw new HttpException(404, "The selected bid was not found for this supplier.");
        if (accept)
        {
            var rfq = await repository.GetRfqAsync(rfqId, cancellationToken);
            if (rfq is null || rfq.Status != RfqStatus.InNegotiation || rfq.AwardedBidId != bidId)
                throw new HttpException(409, "RFQ selection is no longer awaiting this supplier.");
            var contract = await repository.AwardBidAsync(rfqId, bidId, rfq.SelectionNote, rfq.CustomerId, cancellationToken)
                ?? throw new HttpException(409, "RFQ selection is no longer awaiting this supplier.");
            // Supplier acceptance completes the supplier's confirmation step. Move the contract
            // directly to customer review and deposit so the customer does not see a second,
            // contradictory "waiting for supplier" step.
            if (contractService is not null && contract.Status == ContractStatus.Draft)
                await contractService.BeginSignatureAsync(supplierUserId, contract.Id, cancellationToken);
            if (contractService is not null && contract.Status is (ContractStatus.Draft or ContractStatus.PendingSignature))
            {
                var signed = await contractService.SignAsync(supplierUserId, contract.Id, string.Empty, true, null, null, cancellationToken);
                if (realtimePublisher is not null)
                    await realtimePublisher.PublishToUserAsync(rfq.CustomerId, "MarketplaceChanged",
                        new { category = "contract", referenceId = contract.Id, type = "contract.pending_deposit" }, cancellationToken);
                return new(false, signed, "Supplier accepted. The contract is ready for customer review and deposit payment.");
            }
            return new(false, ToContractResponse(contract), "Supplier accepted. The contract is ready for customer review and deposit payment.");
        }
        if (!await repository.DeclineBidSelectionAsync(rfqId, bidId, supplierUserId, cancellationToken))
            throw new HttpException(409, "RFQ selection is no longer awaiting this supplier.");
        return new(false, null, "Supplier declined. The customer can select another quotation.");
    }

    private static ContractResponse ToContractResponse(Contract contract) => new(
        contract.Id, contract.ContractNumber, contract.RfqId, contract.Title, contract.TotalValue,
        contract.DeliveryDeadline, contract.TermsAndConditions, contract.Status,
        contract.SignedByCustomerAt, contract.SignedBySupplierAt, contract.SignedDocumentSha256,
        contract.DepositAmount, contract.DepositPaidAt, contract.CompletedAt);

    private async Task<RfqResponse> ToResponseAsync(RequestForQuotation rfq, bool hideTargetPrice, CancellationToken cancellationToken, bool isInvited = false, bool canDecline = false, Guid? supplierProfileId = null)
    {
        var items = await repository.GetRfqItemsAsync(rfq.Id, cancellationToken);
        var invitations = await repository.GetInvitationsAsync(rfq.Id, cancellationToken);
        var bids = await repository.GetBidsAsync(rfq.Id, cancellationToken: cancellationToken);
        var myBid = supplierProfileId.HasValue
            ? bids.SingleOrDefault(bid => bid.SupplierId == supplierProfileId.Value && bid.Status == BidStatus.Submitted)
            : null;
        var customerName = await repository.GetCustomerNameAsync(rfq.CustomerId, cancellationToken);
        return new RfqResponse(rfq.Id, rfq.RfqNumber, rfq.Title, rfq.Description, rfq.DeliveryAddress, rfq.DeliveryDateRequired,
            rfq.BudgetRangeMin, rfq.BudgetRangeMax, rfq.BidDeadline, rfq.Status,
            items.Select(item => new RfqItemResponse(item.Id, item.ProductName, item.Description, item.Quantity, item.Unit, hideTargetPrice ? null : item.TargetPrice,
                item.Specifications, item.PackagingRequirements, item.NeedsCustomPackaging)).ToArray(),
            invitations.Count, bids.Count, isInvited, canDecline,
            myBid is null ? null : await ToBidResponseAsync(myBid, cancellationToken), customerName, rfq.CreatedAt,
            supplierProfileId.HasValue && rfq.Status == RfqStatus.InNegotiation && rfq.AwardedBidId == myBid?.Id,
            rfq.AwardedBidId);
    }

    private async Task<BidResponse> ToBidResponseAsync(Bid bid, CancellationToken cancellationToken)
    {
        var rfqItems = await repository.GetRfqItemsAsync(bid.RfqId, cancellationToken);
        var byId = rfqItems.ToDictionary(item => item.Id);
        var items = await repository.GetBidItemsAsync(bid.Id, cancellationToken);
        return new BidResponse(bid.Id, bid.BidNumber, bid.SupplierId, bid.TotalPrice, bid.EstimatedDeliveryDays, bid.ValidityDays,
            bid.SubmittedAt, bid.SubmittedAt.AddDays(bid.ValidityDays) <= clock.UtcNow, bid.Status,
            items.Select(item => new BidItemResponse(item.RfqItemId, byId.GetValueOrDefault(item.RfqItemId)?.ProductName,
                item.Quantity, byId.GetValueOrDefault(item.RfqItemId)?.Unit, item.UnitPrice, item.Subtotal, item.Notes)).ToArray());
    }

    private async Task<RequestForQuotation> GetOwnedRfqAsync(Guid customerUserId, Guid rfqId, CancellationToken cancellationToken)
    {
        var rfq = await repository.GetRfqAsync(rfqId, cancellationToken);
        if (rfq is null || rfq.IsDeleted || rfq.CustomerId != customerUserId) throw new HttpException(404, "RFQ was not found.");
        return rfq;
    }

    private async Task<Guid> GetVerifiedSupplierIdAsync(Guid supplierUserId, CancellationToken cancellationToken) =>
        await repository.GetVerifiedSupplierProfileIdAsync(supplierUserId, cancellationToken)
        ?? throw new HttpException(403, "A verified supplier profile is required.");

    private async Task MarkViewedAsync(Guid rfqId, Guid supplierId, CancellationToken cancellationToken)
    {
        var invitation = await repository.GetInvitationAsync(rfqId, supplierId, cancellationToken);
        if (invitation is { Status: RfqInvitationStatus.Invited })
        {
            invitation.Status = RfqInvitationStatus.Viewed;
            await repository.SaveInvitationAsync(invitation, cancellationToken);
        }
    }

    private void EnsureOpen(RequestForQuotation rfq)
    {
        if (rfq.Status != RfqStatus.Open || rfq.BidDeadline <= clock.UtcNow)
            throw new HttpException(409, "The RFQ is closed and no longer accepts responses.");
    }

    private void ValidateRequest(CreateRfqRequest request)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow);
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.DeliveryAddress)
            || request.Items.Count == 0 || request.Items.Any(item => string.IsNullOrWhiteSpace(item.ProductName)
                || string.IsNullOrWhiteSpace(item.Unit) || item.Quantity <= 0))
            throw new HttpException(400, "An RFQ requires a title, delivery address, and at least one valid item.");
        if (request.BudgetRangeMin < 0 || request.BudgetRangeMax < 0
            || request.BudgetRangeMin.HasValue && request.BudgetRangeMax.HasValue && request.BudgetRangeMin > request.BudgetRangeMax)
            throw new HttpException(400, "The RFQ budget range is invalid.");
        if (request.BidDeadline.Kind == DateTimeKind.Unspecified || request.BidDeadline <= clock.UtcNow
            || request.DeliveryDateRequired < today || request.BidDeadline.Date > request.DeliveryDateRequired.ToDateTime(TimeOnly.MinValue))
            throw new HttpException(400, "The bid deadline must be in the future and before the required delivery date.");
    }
}
