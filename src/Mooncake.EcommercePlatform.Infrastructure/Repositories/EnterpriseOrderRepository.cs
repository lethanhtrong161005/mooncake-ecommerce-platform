namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core transactional implementation for enterprise Direct Bulk Order workflows.</summary>
public sealed class EnterpriseOrderRepository(ApplicationDbContext context, IDateTimeProvider clock) : IEnterpriseOrderRepository
{
  private const string OrderAggregateType = "Order";
  private const string OrderReferenceType = "order";

  public async Task<(ShopOrderPolicy? Policy, IReadOnlyList<ShopShippingRate> ShippingRates)> GetShopPolicyAndRatesAsync(
      Guid shopId, CancellationToken cancellationToken = default)
  {
    var policy = await context.ShopOrderPolicies.AsNoTracking()
        .FirstOrDefaultAsync(item => item.ShopId == shopId && !item.IsDeleted, cancellationToken);
    var rates = await context.ShopShippingRates.AsNoTracking()
        .Where(item => item.ShopId == shopId && !item.IsDeleted)
        .OrderBy(item => item.FeeAmount)
        .ThenBy(item => item.RegionCode)
        .ToListAsync(cancellationToken);
    return (policy, rates);
  }

  public async Task<IReadOnlyList<Guid>> GetOwnedShopIdsAsync(Guid supplierUserId, CancellationToken cancellationToken = default) =>
      await (from shop in context.Shops.AsNoTracking()
             join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id into profiles
             from profile in profiles.DefaultIfEmpty()
             where !shop.IsDeleted
                   && (shop.OwnerUserId == supplierUserId || (profile != null && profile.UserId == supplierUserId && !profile.IsDeleted))
             orderby shop.CreatedAt
             select shop.Id).Distinct().ToListAsync(cancellationToken);

  public async Task<ShopOrderPolicyResponse> UpsertShopPolicyAndRatesAsync(
      Guid shopId, Guid supplierUserId, UpsertShopOrderPolicyRequest request, CancellationToken cancellationToken = default)
  {
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    var now = clock.UtcNow;
    var policy = await context.ShopOrderPolicies
        .FirstOrDefaultAsync(item => item.ShopId == shopId && !item.IsDeleted, cancellationToken);

    if (policy is null)
    {
      policy = new ShopOrderPolicy
      {
        Id = Guid.NewGuid(),
        ShopId = shopId,
        DepositEnabled = request.DepositEnabled,
        MinimumOrderTotalForDeposit = decimal.Round(request.MinimumOrderTotalForDeposit, 2, MidpointRounding.AwayFromZero),
        DepositPercent = decimal.Round(request.DepositPercent, 2, MidpointRounding.AwayFromZero),
        TaxIncluded = request.TaxIncluded,
        TaxRate = decimal.Round(request.TaxRate, 2, MidpointRounding.AwayFromZero),
        AllowSelfPickup = request.AllowSelfPickup,
        RequireShippingZoneSelection = request.RequireShippingZoneSelection,
        Version = 1L,
        CreatedBy = supplierUserId,
        CreatedAt = now,
        UpdatedAt = now
      };
      context.ShopOrderPolicies.Add(policy);
    }
    else
    {
      policy.DepositEnabled = request.DepositEnabled;
      policy.MinimumOrderTotalForDeposit = decimal.Round(request.MinimumOrderTotalForDeposit, 2, MidpointRounding.AwayFromZero);
      policy.DepositPercent = decimal.Round(request.DepositPercent, 2, MidpointRounding.AwayFromZero);
      policy.TaxIncluded = request.TaxIncluded;
      policy.TaxRate = decimal.Round(request.TaxRate, 2, MidpointRounding.AwayFromZero);
      policy.AllowSelfPickup = request.AllowSelfPickup;
      policy.RequireShippingZoneSelection = request.RequireShippingZoneSelection;
      policy.Version += 1;
      policy.UpdatedBy = supplierUserId;
      policy.UpdatedAt = now;
    }

    if (request.ShippingRates is not null)
    {
      var existingRates = await context.ShopShippingRates
          .Where(item => item.ShopId == shopId && !item.IsDeleted)
          .ToListAsync(cancellationToken);
      var incomingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach (var incoming in request.ShippingRates)
      {
        var normalizedCode = incoming.RegionCode.Trim().ToUpperInvariant();
        if (!incomingCodes.Add(normalizedCode))
          throw new HttpException(400, $"Duplicate shipping region code '{normalizedCode}'.", OrderErrorCodes.ValidationError);

        var match = existingRates.FirstOrDefault(item => string.Equals(item.RegionCode, normalizedCode, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
          context.ShopShippingRates.Add(new ShopShippingRate
          {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            RegionCode = normalizedCode,
            RegionName = incoming.RegionName.Trim(),
            FeeAmount = decimal.Round(incoming.FeeAmount, 2, MidpointRounding.AwayFromZero),
            FreeShippingThreshold = incoming.FreeShippingThreshold.HasValue
                  ? decimal.Round(incoming.FreeShippingThreshold.Value, 2, MidpointRounding.AwayFromZero)
                  : null,
            EstimatedDays = incoming.EstimatedDays,
            IsSupported = incoming.IsSupported,
            CreatedBy = supplierUserId,
            CreatedAt = now,
            UpdatedAt = now
          });
        }
        else
        {
          match.RegionCode = normalizedCode;
          match.RegionName = incoming.RegionName.Trim();
          match.FeeAmount = decimal.Round(incoming.FeeAmount, 2, MidpointRounding.AwayFromZero);
          match.FreeShippingThreshold = incoming.FreeShippingThreshold.HasValue
              ? decimal.Round(incoming.FreeShippingThreshold.Value, 2, MidpointRounding.AwayFromZero)
              : null;
          match.EstimatedDays = incoming.EstimatedDays;
          match.IsSupported = incoming.IsSupported;
          match.UpdatedBy = supplierUserId;
          match.UpdatedAt = now;
        }
      }

      foreach (var stale in existingRates.Where(item => !incomingCodes.Contains(item.RegionCode)))
      {
        stale.IsDeleted = true;
        stale.UpdatedBy = supplierUserId;
        stale.UpdatedAt = now;
      }
    }

    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);

    var finalRates = await context.ShopShippingRates.AsNoTracking()
        .Where(item => item.ShopId == shopId && !item.IsDeleted)
        .OrderBy(item => item.FeeAmount)
        .ThenBy(item => item.RegionCode)
        .ToListAsync(cancellationToken);

    return MapShopPolicy(shopId, policy, finalRates);
  }

  public async Task RecordInitialReservationAsync(Order order, IReadOnlyList<OrderItem> items, CancellationToken cancellationToken = default)
  {
    var existing = await context.InventoryReservations.AsNoTracking()
        .AnyAsync(item => item.OrderId == order.Id, cancellationToken);
    if (existing)
      return;

    var now = clock.UtcNow;
    foreach (var group in items.GroupBy(item => new { item.ProductId, item.VariantId }).OrderBy(group => group.Key.VariantId))
    {
      context.InventoryReservations.Add(new InventoryReservation
      {
        Id = Guid.NewGuid(),
        OrderId = order.Id,
        ProductId = group.Key.ProductId,
        VariantId = group.Key.VariantId,
        ShopId = order.ShopId,
        Quantity = group.Sum(item => item.Quantity),
        Status = "Active",
        ExpiresAt = order.ReservationExpiresAt,
        CreatedBy = order.CustomerId,
        CreatedAt = now,
        UpdatedAt = now
      });
    }

    await context.SaveChangesAsync(cancellationToken);
  }

  public async Task<bool> EnsurePaymentScheduleOnConfirmAsync(
      Order order, Guid actorUserId, DateTime confirmedAtUtc, DateOnly? confirmedDeliveryDate,
      CancellationToken cancellationToken = default)
  {
    context.ChangeTracker.Clear();
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    var lockedOrder = await context.Orders.FromSqlInterpolated(
            $"SELECT * FROM orders WHERE id = {order.Id} FOR UPDATE")
        .SingleOrDefaultAsync(cancellationToken);
    if (lockedOrder is null || lockedOrder.IsDeleted)
      return false;

    var depositDeadline = confirmedAtUtc.AddHours(OrderPolicyDefaults.DepositPaymentSlaHours);
    lockedOrder.ConfirmedAt ??= confirmedAtUtc;
    lockedOrder.ConfirmedDeliveryDate = confirmedDeliveryDate ?? lockedOrder.DeliveryDateExpected;
    lockedOrder.ReservationExpiresAt = depositDeadline;
    lockedOrder.Version += 1;
    lockedOrder.UpdatedAt = confirmedAtUtc;
    lockedOrder.UpdatedBy = actorUserId;

    await context.InventoryReservations
        .Where(item => item.OrderId == lockedOrder.Id && item.Status == "Active")
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.ExpiresAt, depositDeadline)
            .SetProperty(item => item.UpdatedAt, confirmedAtUtc), cancellationToken);

    var hasSchedule = await context.OrderPaymentObligations
        .AnyAsync(item => item.OrderId == lockedOrder.Id && !item.IsDeleted, cancellationToken);
    if (!hasSchedule)
    {
      BuildInitialObligationsAndParts(lockedOrder, actorUserId, confirmedAtUtc, depositDeadline);
    }

    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);

    order.ConfirmedAt = lockedOrder.ConfirmedAt;
    order.ConfirmedDeliveryDate = lockedOrder.ConfirmedDeliveryDate;
    order.ReservationExpiresAt = lockedOrder.ReservationExpiresAt;
    order.Version = lockedOrder.Version;
    return true;
  }

  public async Task<bool> ProposeDeliveryDateAsync(
      Guid orderId, Guid supplierUserId, DateOnly proposedDate, string reason, long? expectedVersion,
      CancellationToken cancellationToken = default)
  {
    context.ChangeTracker.Clear();
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    var lockedOrder = await context.Orders.FromSqlInterpolated(
            $"SELECT * FROM orders WHERE id = {orderId} FOR UPDATE")
        .SingleOrDefaultAsync(cancellationToken);
    if (lockedOrder is null || lockedOrder.IsDeleted || lockedOrder.Status != OrderStatus.Pending)
      return false;
    if (expectedVersion.HasValue && lockedOrder.Version != expectedVersion.Value)
      return false;

    var now = clock.UtcNow;
    var approvalDeadline = now.AddHours(OrderPolicyDefaults.CustomerDeliveryProposalSlaHours);
    lockedOrder.Status = OrderStatus.AwaitingCustomerDeliveryApproval;
    lockedOrder.ProposedDeliveryDate = proposedDate;
    lockedOrder.DeliveryProposalReason = reason.Trim();
    lockedOrder.DeliveryApprovalDeadlineAt = approvalDeadline;
    lockedOrder.ReservationExpiresAt = approvalDeadline;
    lockedOrder.Version += 1;
    lockedOrder.UpdatedAt = now;
    lockedOrder.UpdatedBy = supplierUserId;

    await context.InventoryReservations
        .Where(item => item.OrderId == lockedOrder.Id && item.Status == "Active")
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.ExpiresAt, approvalDeadline)
            .SetProperty(item => item.UpdatedAt, now), cancellationToken);

    context.WorkflowEvents.Add(new WorkflowEvent
    {
      AggregateType = OrderAggregateType,
      AggregateId = lockedOrder.Id,
      EventType = "DeliveryDateProposed",
      FromStatus = OrderStatus.Pending.ToString(),
      ToStatus = OrderStatus.AwaitingCustomerDeliveryApproval.ToString(),
      ActorUserId = supplierUserId,
      Reason = reason.Trim()
    });
    context.Notifications.Add(new Notification
    {
      UserId = lockedOrder.CustomerId,
      Type = "order.delivery_date_proposed",
      Title = "Supplier proposed a new delivery date",
      Content = $"Order {lockedOrder.OrderNumber} has a proposed delivery date of {proposedDate:yyyy-MM-dd}. Respond within 12 hours.",
      ReferenceType = OrderReferenceType,
      ReferenceId = lockedOrder.Id
    });

    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return true;
  }

  public async Task<bool> RespondDeliveryProposalAsync(
      Guid orderId, Guid customerId, bool approve, string? reason, long? expectedVersion,
      CancellationToken cancellationToken = default)
  {
    context.ChangeTracker.Clear();
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    var lockedOrder = await context.Orders.FromSqlInterpolated(
            $"SELECT * FROM orders WHERE id = {orderId} FOR UPDATE")
        .SingleOrDefaultAsync(cancellationToken);
    if (lockedOrder is null || lockedOrder.IsDeleted || lockedOrder.CustomerId != customerId ||
        lockedOrder.Status != OrderStatus.AwaitingCustomerDeliveryApproval)
      return false;
    if (expectedVersion.HasValue && lockedOrder.Version != expectedVersion.Value)
      return false;

    var now = clock.UtcNow;
    var supplierUserId = await GetSupplierUserIdByShopAsync(lockedOrder.ShopId, cancellationToken);

    if (approve)
    {
      var depositDeadline = now.AddHours(OrderPolicyDefaults.DepositPaymentSlaHours);
      lockedOrder.Status = OrderStatus.Confirmed;
      lockedOrder.ConfirmedAt = now;
      lockedOrder.ConfirmedDeliveryDate = lockedOrder.ProposedDeliveryDate ?? lockedOrder.DeliveryDateExpected;
      lockedOrder.DeliveryDateExpected = lockedOrder.ConfirmedDeliveryDate;
      lockedOrder.DeliveryApprovalDeadlineAt = null;
      lockedOrder.ReservationExpiresAt = depositDeadline;
      lockedOrder.Version += 1;
      lockedOrder.UpdatedAt = now;
      lockedOrder.UpdatedBy = customerId;

      await context.InventoryReservations
          .Where(item => item.OrderId == lockedOrder.Id && item.Status == "Active")
          .ExecuteUpdateAsync(setters => setters
              .SetProperty(item => item.ExpiresAt, depositDeadline)
              .SetProperty(item => item.UpdatedAt, now), cancellationToken);

      var hasSchedule = await context.OrderPaymentObligations
          .AnyAsync(item => item.OrderId == lockedOrder.Id && !item.IsDeleted, cancellationToken);
      if (!hasSchedule)
        BuildInitialObligationsAndParts(lockedOrder, customerId, now, depositDeadline);

      context.WorkflowEvents.Add(new WorkflowEvent
      {
        AggregateType = OrderAggregateType,
        AggregateId = lockedOrder.Id,
        EventType = "DeliveryProposalApproved",
        FromStatus = OrderStatus.AwaitingCustomerDeliveryApproval.ToString(),
        ToStatus = OrderStatus.Confirmed.ToString(),
        ActorUserId = customerId,
        Reason = reason
      });
      if (supplierUserId != Guid.Empty)
      {
        context.Notifications.Add(new Notification
        {
          UserId = supplierUserId,
          Type = "order.delivery_proposal_approved",
          Title = "Customer approved proposed delivery date",
          Content = $"Order {lockedOrder.OrderNumber} is now confirmed for {lockedOrder.ConfirmedDeliveryDate:yyyy-MM-dd}.",
          ReferenceType = OrderReferenceType,
          ReferenceId = lockedOrder.Id
        });
      }
    }
    else
    {
      var cancelReason = string.IsNullOrWhiteSpace(reason)
          ? "Customer declined the proposed delivery date."
          : $"Customer declined proposed delivery date: {reason.Trim()}";
      lockedOrder.Status = OrderStatus.Cancelled;
      lockedOrder.CancelledAt = now;
      lockedOrder.CancelledByUserId = customerId;
      lockedOrder.CancellationReason = cancelReason;
      lockedOrder.ReservationExpiresAt = null;
      lockedOrder.Version += 1;
      lockedOrder.UpdatedAt = now;
      lockedOrder.UpdatedBy = customerId;

      await ReleaseOrderStockIdempotentCoreAsync(lockedOrder, customerId, cancelReason, now, cancellationToken);

      context.WorkflowEvents.Add(new WorkflowEvent
      {
        AggregateType = OrderAggregateType,
        AggregateId = lockedOrder.Id,
        EventType = "DeliveryProposalDeclined",
        FromStatus = OrderStatus.AwaitingCustomerDeliveryApproval.ToString(),
        ToStatus = OrderStatus.Cancelled.ToString(),
        ActorUserId = customerId,
        Reason = cancelReason
      });
      if (supplierUserId != Guid.Empty)
      {
        context.Notifications.Add(new Notification
        {
          UserId = supplierUserId,
          Type = "order.cancelled",
          Title = "Customer declined proposed delivery date",
          Content = $"Order {lockedOrder.OrderNumber} was cancelled because the proposed delivery date was declined.",
          ReferenceType = OrderReferenceType,
          ReferenceId = lockedOrder.Id
        });
      }
    }

    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return true;
  }

  public async Task<(bool AllowedToStartProduction, bool HasUnapprovedPackaging, bool HasUnpaidPreProductionObligation)>
      EvaluateProductionReadinessAsync(Guid orderId, CancellationToken cancellationToken = default)
  {
    var packagingStatus = await context.CustomPackagings.AsNoTracking()
        .Where(item => item.OrderId == orderId && !item.IsDeleted)
        .Select(item => (CustomPackagingStatus?)item.Status)
        .FirstOrDefaultAsync(cancellationToken);
    var hasUnapprovedPackaging = packagingStatus.HasValue && packagingStatus.Value != CustomPackagingStatus.Approved;

    var obligations = await context.OrderPaymentObligations.AsNoTracking()
        .Where(item => item.OrderId == orderId && !item.IsDeleted && item.Status != "Cancelled")
        .ToListAsync(cancellationToken);

    bool hasUnpaidPreProd;
    if (obligations.Count > 0)
    {
      hasUnpaidPreProd = obligations.Any(item => item.IsRequiredBeforeProduction && item.Status != "Paid");
    }
    else
    {
      var hasCompletedPayment = await context.Payments.AsNoTracking()
          .AnyAsync(item => item.OrderId == orderId && item.Status == PaymentStatus.Completed && !item.IsDeleted, cancellationToken);
      hasUnpaidPreProd = !hasCompletedPayment;
    }

    return (!hasUnapprovedPackaging && !hasUnpaidPreProd, hasUnapprovedPackaging, hasUnpaidPreProd);
  }

  public async Task<bool> HasUnpaidPreShippingObligationAsync(Guid orderId, CancellationToken cancellationToken = default)
  {
    var obligations = await context.OrderPaymentObligations.AsNoTracking()
        .Where(item => item.OrderId == orderId && !item.IsDeleted && item.Status != "Cancelled")
        .ToListAsync(cancellationToken);
    if (obligations.Count == 0)
      return false;
    return obligations.Any(item => item.IsRequiredBeforeShipping && item.Status != "Paid");
  }

  public async Task CommitInventoryReservationsAsync(Guid orderId, CancellationToken cancellationToken = default)
  {
    var now = clock.UtcNow;
    await context.InventoryReservations
        .Where(item => item.OrderId == orderId && item.Status == "Active")
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.Status, "Committed")
            .SetProperty(item => item.ExpiresAt, (DateTime?)null)
            .SetProperty(item => item.UpdatedAt, now), cancellationToken);
    await context.Orders
        .Where(item => item.Id == orderId)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.ReservationExpiresAt, (DateTime?)null), cancellationToken);
  }

  public async Task<OrderResponse> EnrichOrderResponseAsync(
      OrderResponse baseResponse, Order order, Guid viewerUserId, bool viewerIsSupplier, bool viewerIsAdmin,
      CancellationToken cancellationToken = default)
  {
    var batch = await EnrichOrderResponsesBatchAsync(
        [(order, baseResponse)], viewerUserId, viewerIsSupplier, viewerIsAdmin, cancellationToken);
    return batch[0];
  }

  public async Task<IReadOnlyList<OrderResponse>> EnrichOrderResponsesBatchAsync(
      IReadOnlyList<(Order Order, OrderResponse BaseResponse)> items,
      Guid viewerUserId, bool viewerIsSupplier, bool viewerIsAdmin, CancellationToken cancellationToken = default)
  {
    if (items.Count == 0)
      return [];

    var orderIds = items.Select(item => item.Order.Id).Distinct().ToArray();
    var shopIds = items.Select(item => item.Order.ShopId).Distinct().ToArray();
    var customerIds = items.Select(item => item.Order.CustomerId).Distinct().ToArray();

    var shopNames = await context.Shops.AsNoTracking()
        .Where(shop => shopIds.Contains(shop.Id))
        .ToDictionaryAsync(shop => shop.Id, shop => shop.Name, cancellationToken);

    var customerNames = await context.Users.AsNoTracking()
        .Where(user => customerIds.Contains(user.Id))
        .ToDictionaryAsync(user => user.Id, user => user.FullName, cancellationToken);

    var obligations = await context.OrderPaymentObligations.AsNoTracking()
        .Where(item => orderIds.Contains(item.OrderId) && !item.IsDeleted)
        .OrderBy(item => item.SequenceNo)
        .ToListAsync(cancellationToken);

    var parts = await context.OrderPaymentParts.AsNoTracking()
        .Where(item => orderIds.Contains(item.OrderId) && !item.IsDeleted)
        .OrderBy(item => item.PartIndex)
        .ToListAsync(cancellationToken);

    var payments = await context.Payments.AsNoTracking()
        .Where(item => item.OrderId.HasValue && orderIds.Contains(item.OrderId.Value) && !item.IsDeleted)
        .OrderByDescending(item => item.CreatedAt)
        .ToListAsync(cancellationToken);

    var ledgerEntries = await context.OrderFinancialLedgerEntries.AsNoTracking()
        .Where(item => orderIds.Contains(item.OrderId) && !item.IsDeleted)
        .OrderByDescending(item => item.CreatedAt)
        .ToListAsync(cancellationToken);

    var refundCases = await context.OrderRefundCases.AsNoTracking()
        .Where(item => orderIds.Contains(item.OrderId) && !item.IsDeleted)
        .OrderByDescending(item => item.CreatedAt)
        .ToListAsync(cancellationToken);

    var payoutRequests = await context.SupplierPayoutRequests.AsNoTracking()
        .Where(item => orderIds.Contains(item.OrderId) && !item.IsDeleted)
        .OrderByDescending(item => item.CreatedAt)
        .ToListAsync(cancellationToken);

    var feeChangeRequests = await context.PackagingFeeChangeRequests.AsNoTracking()
        .Where(item => orderIds.Contains(item.OrderId) && !item.IsDeleted)
        .OrderByDescending(item => item.CreatedAt)
        .ToListAsync(cancellationToken);

    var cancelRequests = await context.OrderCancellationRequests.AsNoTracking()
        .Where(item => orderIds.Contains(item.OrderId) && !item.IsDeleted && item.Status == "Pending")
        .OrderByDescending(item => item.CreatedAt)
        .ToListAsync(cancellationToken);

    var activeDisputes = await context.OrderDeliveryDisputes.AsNoTracking()
        .Where(item => orderIds.Contains(item.OrderId) && !item.IsDeleted && (item.Status == "Open" || item.Status == "AwaitingSupplierRebuttal"))
        .OrderByDescending(item => item.CreatedAt)
        .ToListAsync(cancellationToken);

    var deliveries = await context.Deliveries.AsNoTracking()
        .Where(item => item.OrderId.HasValue && orderIds.Contains(item.OrderId.Value) && !item.IsDeleted)
        .OrderByDescending(item => item.CreatedAt)
        .ToListAsync(cancellationToken);

    var deliveryIds = deliveries.Select(item => item.Id).ToArray();
    var proofs = deliveryIds.Length == 0
        ? []
        : await context.DeliveryProofs.AsNoTracking()
            .Where(item => deliveryIds.Contains(item.DeliveryId) && !item.IsDeleted)
            .OrderByDescending(item => item.TakenAt)
            .ToListAsync(cancellationToken);

    var obligationsByOrder = obligations.GroupBy(x => x.OrderId).ToDictionary(g => g.Key, g => g.ToList());
    var partsByObligation = parts.GroupBy(x => x.ObligationId).ToDictionary(g => g.Key, g => g.ToList());
    var paymentsByOrder = payments.GroupBy(x => x.OrderId!.Value).ToDictionary(g => g.Key, g => g.ToList());
    var ledgerByOrder = ledgerEntries.GroupBy(x => x.OrderId).ToDictionary(g => g.Key, g => g.ToList());
    var refundsByOrder = refundCases.GroupBy(x => x.OrderId).ToDictionary(g => g.Key, g => g.ToList());
    var payoutsByOrder = payoutRequests.GroupBy(x => x.OrderId).ToDictionary(g => g.Key, g => g.ToList());
    var feeChangesByOrder = feeChangeRequests.GroupBy(x => x.OrderId).ToDictionary(g => g.Key, g => g.ToList());
    var pendingCancelByOrder = cancelRequests.GroupBy(x => x.OrderId).ToDictionary(g => g.Key, g => g.First());
    var disputeByOrder = activeDisputes.GroupBy(x => x.OrderId).ToDictionary(g => g.Key, g => g.First());
    var latestDeliveryByOrder = deliveries.GroupBy(x => x.OrderId!.Value).ToDictionary(g => g.Key, g => g.First());
    var proofsByDelivery = proofs.GroupBy(x => x.DeliveryId).ToDictionary(g => g.Key, g => g.ToList());

    var enriched = new List<OrderResponse>(items.Count);
    foreach (var (order, baseResp) in items)
    {
      var orderObligations = obligationsByOrder.GetValueOrDefault(order.Id, []);
      var orderPayments = paymentsByOrder.GetValueOrDefault(order.Id, []);
      var orderLedger = ledgerByOrder.GetValueOrDefault(order.Id, []);
      var orderRefunds = refundsByOrder.GetValueOrDefault(order.Id, []);
      var orderPayouts = payoutsByOrder.GetValueOrDefault(order.Id, []);
      var orderFeeChanges = feeChangesByOrder.GetValueOrDefault(order.Id, []);
      pendingCancelByOrder.TryGetValue(order.Id, out var pendingCancel);
      disputeByOrder.TryGetValue(order.Id, out var activeDispute);
      latestDeliveryByOrder.TryGetValue(order.Id, out var latestDelivery);

      var paymentSummary = BuildPaymentSummary(order, baseResp.CustomPackaging, orderObligations, partsByObligation, orderPayments);
      var financialSummary = BuildFinancialSummary(order, orderLedger, orderRefunds, orderPayouts);
      var deliverySnapshot = latestDelivery is null
          ? null
          : new OrderDeliverySnapshotResponse(
              latestDelivery.Id,
              latestDelivery.DeliveryNumber,
              latestDelivery.Status,
              latestDelivery.TrackingCode,
              latestDelivery.ScheduledDate,
              latestDelivery.ActualDeliveryDate,
              proofsByDelivery.GetValueOrDefault(latestDelivery.Id, []).Count,
              proofsByDelivery.GetValueOrDefault(latestDelivery.Id, [])
                  .Select(p => new OrderDeliveryProofPhotoResponse(p.Id, p.ProofType, p.PhotoUrl, p.TakenBy, p.TakenAt ?? p.CreatedAt, p.Notes))
                  .ToList());

      var allowedActions = ComputeAllowedActions(
          order, baseResp.CustomPackaging, paymentSummary, financialSummary, deliverySnapshot,
          pendingCancel, activeDispute, viewerUserId, viewerIsSupplier, viewerIsAdmin);

      DateTime? autoConfirmAt = null;
      if (order.Status == OrderStatus.Shipped && latestDelivery is not null)
      {
        var deliveryProofs = proofsByDelivery.GetValueOrDefault(latestDelivery.Id, []);
        var earliestDeliveryProof = deliveryProofs
            .Where(p => p.ProofType == ProofType.Delivery && !p.IsDeleted)
            .OrderBy(p => p.CreatedAt)
            .FirstOrDefault();
        if (earliestDeliveryProof is not null)
        {
          autoConfirmAt = earliestDeliveryProof.CreatedAt.AddHours(OrderPolicyDefaults.DeliveryAutoConfirmHours);
        }
      }

      enriched.Add(baseResp with
      {
        Version = order.Version,
        ShopName = shopNames.GetValueOrDefault(order.ShopId),
        CustomerName = customerNames.GetValueOrDefault(order.CustomerId),
        ResponseDeadlineAtUtc = order.ResponseDeadlineAt,
        ReservationExpiresAtUtc = order.ReservationExpiresAt,
        ConfirmedAtUtc = order.ConfirmedAt,
        ConfirmedDeliveryDate = order.ConfirmedDeliveryDate,
        ProposedDeliveryDate = order.ProposedDeliveryDate,
        DeliveryProposalReason = order.DeliveryProposalReason,
        DeliveryApprovalDeadlineAtUtc = order.DeliveryApprovalDeadlineAt,
        AutoConfirmAt = autoConfirmAt,
        CancelledAtUtc = order.CancelledAt,
        CancellationReason = order.CancellationReason,
        PriceSnapshotVersion = order.PriceSnapshotVersion,
        ShippingZoneCode = order.ShippingZoneCode,
        ShippingZoneName = order.ShippingZoneNameSnapshot,
        TaxIncluded = order.TaxIncludedSnapshot,
        TaxRate = order.TaxRateSnapshot,
        DepositRequired = order.DepositRequiredSnapshot,
        DepositPercent = order.DepositPercentSnapshot,
        CanStartProduction = paymentSummary.CanStartProduction,
        CanMarkShipped = paymentSummary.CanMarkShipped,
        AllowedActions = allowedActions,
        PaymentSummary = paymentSummary,
        FinancialSummary = financialSummary,
        LatestDelivery = deliverySnapshot,
        FeeChangeRequests = orderFeeChanges.Select(MapFeeChange).ToList(),
        PendingCancellationRequest = pendingCancel is null ? null : MapCancelRequest(pendingCancel),
        ActiveDispute = activeDispute is null ? null : MapDispute(activeDispute, order.OrderNumber, paymentSummary.TotalPaidAmount)
      });
    }

    return enriched;
  }

  public async Task<(IReadOnlyList<Order> Orders, string? NextCursor, bool HasMore)> QueryOrdersCursorAsync(
      Guid userId, bool supplier, OrderCursorQueryRequest query, CancellationToken cancellationToken = default)
  {
    var limit = Math.Clamp(query.Limit, 1, 100);
    IQueryable<Order> baseQuery;

    if (supplier)
    {
      var ownedShopIds = await GetOwnedShopIdsAsync(userId, cancellationToken);
      if (ownedShopIds.Count == 0)
        return ([], null, false);
      baseQuery = context.Orders.AsNoTracking()
          .Where(order => ownedShopIds.Contains(order.ShopId) && !order.IsDeleted);
    }
    else
    {
      baseQuery = context.Orders.AsNoTracking()
          .Where(order => order.CustomerId == userId && !order.IsDeleted);
    }

    if (query.Status.HasValue)
      baseQuery = baseQuery.Where(order => order.Status == query.Status.Value);

    if (TryDecodeCursor(query.Cursor, out var cursorCreatedAt, out var cursorId))
    {
      baseQuery = baseQuery.Where(order =>
          order.CreatedAt < cursorCreatedAt ||
          (order.CreatedAt == cursorCreatedAt && order.Id.CompareTo(cursorId) < 0));
    }

    var rows = await baseQuery
        .OrderByDescending(order => order.CreatedAt)
        .ThenByDescending(order => order.Id)
        .Take(limit + 1)
        .ToListAsync(cancellationToken);

    var hasMore = rows.Count > limit;
    var pageItems = hasMore ? rows.Take(limit).ToList() : rows;
    var nextCursor = hasMore
        ? EncodeCursor(pageItems[^1].CreatedAt, pageItems[^1].Id)
        : null;
    return (pageItems, nextCursor, hasMore);
  }

  public async Task<PagedResult<Order>> QueryOrdersPageAsync(Guid userId, bool supplier,
      OrderPageQueryRequest query, CancellationToken cancellationToken = default)
  {
    IQueryable<Order> rows = context.Orders.AsNoTracking().Where(order => !order.IsDeleted);
    if (supplier)
    {
      var shopIds = await GetOwnedShopIdsAsync(userId, cancellationToken);
      rows = rows.Where(order => shopIds.Contains(order.ShopId));
    }
    else
    {
      rows = rows.Where(order => order.CustomerId == userId);
    }

    if (query.Status is { Length: > 0 })
      rows = rows.Where(order => query.Status.Contains(order.Status));
    if (query.ShopId.HasValue)
      rows = rows.Where(order => order.ShopId == query.ShopId.Value);
    if (!string.IsNullOrWhiteSpace(query.Q))
    {
      var term = $"%{query.Q.Trim()}%";
      rows = rows.Where(order => EF.Functions.ILike(order.OrderNumber, term)
          || (order.RecipientName != null && EF.Functions.ILike(order.RecipientName, term)));
    }
    if (query.From.HasValue)
      rows = rows.Where(order => order.CreatedAt >= query.From.Value);
    if (query.To.HasValue)
      rows = rows.Where(order => order.CreatedAt <= query.To.Value);
    if (query.HasPackaging.HasValue)
      rows = rows.Where(order => context.CustomPackagings.Any(packaging =>
          packaging.OrderId == order.Id && !packaging.IsDeleted) == query.HasPackaging.Value);
    if (query.PaymentStatus.HasValue)
      rows = rows.Where(order => context.Payments.Any(payment =>
          payment.OrderId == order.Id && !payment.IsDeleted && payment.Status == query.PaymentStatus.Value));
    if (query.NeedsAction == true)
    {
      rows = supplier
          ? rows.Where(order => order.Status == OrderStatus.Pending || order.Status == OrderStatus.CancellationRequested)
          : rows.Where(order => order.Status == OrderStatus.AwaitingCustomerDeliveryApproval
              || order.Status == OrderStatus.Shipped
              || ((order.Status == OrderStatus.Confirmed || order.Status == OrderStatus.Processing)
                  && context.OrderPaymentObligations.Any(obligation => obligation.OrderId == order.Id
                      && !obligation.IsDeleted && obligation.PaidAmount < obligation.TotalAmount)));
    }
    else if (query.NeedsAction == false)
    {
      rows = supplier
          ? rows.Where(order => order.Status != OrderStatus.Pending && order.Status != OrderStatus.CancellationRequested)
          : rows.Where(order => order.Status != OrderStatus.AwaitingCustomerDeliveryApproval
              && order.Status != OrderStatus.Shipped
              && !((order.Status == OrderStatus.Confirmed || order.Status == OrderStatus.Processing)
                  && context.OrderPaymentObligations.Any(obligation => obligation.OrderId == order.Id
                      && !obligation.IsDeleted && obligation.PaidAmount < obligation.TotalAmount)));
    }

    var totalCount = await rows.CountAsync(cancellationToken);
    rows = query.Sort switch
    {
      "createdAtAsc" => rows.OrderBy(order => order.CreatedAt).ThenBy(order => order.Id),
      "totalAmountAsc" => rows.OrderBy(order => order.TotalAmount).ThenBy(order => order.Id),
      "totalAmountDesc" => rows.OrderByDescending(order => order.TotalAmount).ThenByDescending(order => order.Id),
      "deadlineAsc" => rows.OrderBy(order => order.ResponseDeadlineAt ?? order.ReservationExpiresAt)
          .ThenBy(order => order.Id),
      _ => rows.OrderByDescending(order => order.CreatedAt).ThenByDescending(order => order.Id)
    };
    var items = await PagedQueryHelper.ApplyPaging(rows, query).ToListAsync(cancellationToken);
    return new PagedResult<Order>(items, query.Page, query.PageSize, totalCount);
  }

  public async Task<SupplierOrderDashboardSummaryResponse> GetSupplierOrderDashboardSummaryAsync(Guid userId,
      CancellationToken cancellationToken = default)
  {
    var shopIds = await GetOwnedShopIdsAsync(userId, cancellationToken);
    var supplierProfileIds = await context.SupplierProfiles.AsNoTracking()
        .Where(profile => profile.UserId == userId && !profile.IsDeleted)
        .Select(profile => profile.Id)
        .ToListAsync(cancellationToken);

    var orderSummaries = shopIds.Count == 0
        ? []
        : await context.Orders.AsNoTracking()
            .Where(order => !order.IsDeleted && shopIds.Contains(order.ShopId))
            .Select(order => new { order.Id, order.Status, order.TotalAmount, order.CreatedAt })
            .ToListAsync(cancellationToken);

    var recognizedOrderIds = orderSummaries
        .Where(order => order.Status is OrderStatus.Confirmed
            or OrderStatus.AwaitingCustomerDeliveryApproval
            or OrderStatus.Processing
            or OrderStatus.Shipped
            or OrderStatus.Delivered)
        .Select(order => order.Id)
        .ToHashSet();

    var ledgerEntries = shopIds.Count == 0
        ? []
        : await context.OrderFinancialLedgerEntries.AsNoTracking()
            .Where(entry => !entry.IsDeleted && shopIds.Contains(entry.ShopId)
                && (entry.EntryType == "Captured" || entry.EntryType == "RefundedSimulated"))
            .Select(entry => new { entry.OrderId, entry.EntryType, entry.Amount, entry.CreatedAt })
            .ToListAsync(cancellationToken);

    var directCapturedByOrder = ledgerEntries
        .Where(entry => entry.EntryType == "Captured")
        .GroupBy(entry => entry.OrderId)
        .ToDictionary(group => group.Key, group => group.Sum(item => item.Amount));

    var directRefundedByOrder = ledgerEntries
        .Where(entry => entry.EntryType == "RefundedSimulated")
        .GroupBy(entry => entry.OrderId)
        .ToDictionary(group => group.Key, group => group.Sum(item => item.Amount));

    var recognizedDirectRefunds = recognizedOrderIds.Sum(id => directRefundedByOrder.GetValueOrDefault(id));
    var grossDirectRevenue = orderSummaries
        .Where(order => recognizedOrderIds.Contains(order.Id))
        .Sum(order => order.TotalAmount);
    var directOrderRevenue = Math.Max(0m, grossDirectRevenue - recognizedDirectRefunds);

    var totalOrderCaptured = ledgerEntries.Where(entry => entry.EntryType == "Captured").Sum(entry => entry.Amount);
    var totalOrderRefunded = ledgerEntries.Where(entry => entry.EntryType == "RefundedSimulated").Sum(entry => entry.Amount);
    var directOrderPaidRevenue = Math.Max(0m, totalOrderCaptured - totalOrderRefunded);

    var packagingCount = shopIds.Count == 0
        ? 0
        : await context.Orders.AsNoTracking()
            .Where(order => !order.IsDeleted && shopIds.Contains(order.ShopId))
            .CountAsync(order => context.CustomPackagings.Any(packaging =>
                packaging.OrderId == order.Id && !packaging.IsDeleted), cancellationToken);

    // RFQ -> B2B Contract stream: count Active / CompletedOnTime / CompletedLate contracts.
    var contractSummaries = supplierProfileIds.Count == 0
        ? []
        : await context.Contracts.AsNoTracking()
            .Where(contract => !contract.IsDeleted && supplierProfileIds.Contains(contract.SupplierId))
            .Select(contract => new { contract.Id, contract.Status, contract.TotalValue, contract.CreatedAt })
            .ToListAsync(cancellationToken);

    var recognizedContracts = contractSummaries
        .Where(contract => contract.Status is ContractStatus.Active
            or ContractStatus.CompletedOnTime
            or ContractStatus.CompletedLate)
        .ToList();
    var recognizedContractIds = recognizedContracts.Select(contract => contract.Id).ToList();
    var allContractIds = contractSummaries.Select(contract => contract.Id).ToList();

    var contractDisputeRefunds = allContractIds.Count == 0
        ? []
        : await context.ContractDisputes.AsNoTracking()
            .Where(dispute => !dispute.IsDeleted && allContractIds.Contains(dispute.ContractId)
                && dispute.Status == "Resolved" && dispute.RefundAmount > 0m)
            .Select(dispute => new { dispute.ContractId, dispute.RefundAmount })
            .ToListAsync(cancellationToken);

    var recognizedContractRefundById = contractDisputeRefunds
        .Where(dispute => recognizedContractIds.Contains(dispute.ContractId))
        .GroupBy(dispute => dispute.ContractId)
        .ToDictionary(group => group.Key, group => group.Sum(item => item.RefundAmount));
    var recognizedContractRefunds = recognizedContractRefundById.Values.Sum();
    var allContractRefunds = contractDisputeRefunds.Sum(dispute => dispute.RefundAmount);

    var contractRevenue = Math.Max(0m,
        recognizedContracts.Sum(contract => contract.TotalValue ?? 0m) - recognizedContractRefunds);

    var paidMilestonesList = recognizedContractIds.Count == 0
        ? []
        : await context.ContractMilestones.AsNoTracking()
            .Where(milestone => !milestone.IsDeleted && recognizedContractIds.Contains(milestone.ContractId)
                && milestone.Status == MilestoneStatus.Paid)
            .Select(milestone => new { milestone.ContractId, Amount = milestone.Amount ?? 0m })
            .ToListAsync(cancellationToken);
    var contractPaidById = paidMilestonesList
        .GroupBy(item => item.ContractId)
        .ToDictionary(group => group.Key, group => group.Sum(item => item.Amount));
    var contractPaidMilestones = paidMilestonesList.Sum(item => item.Amount);
    var contractPaidRevenue = Math.Max(0m, contractPaidMilestones - recognizedContractRefunds);

    // Build daily (14 days) and monthly (6 months) series in UTC+7 (Vietnam time) combining both flows.
    DateTime ToVnTime(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(7);
    var nowVn = ToVnTime(clock.UtcNow);
    var todayVn = nowVn.Date;

    var recognizedOrderRows = orderSummaries
        .Where(order => recognizedOrderIds.Contains(order.Id))
        .Select(order => new
        {
          VnTime = ToVnTime(order.CreatedAt),
          NetRevenue = Math.Max(0m, order.TotalAmount - directRefundedByOrder.GetValueOrDefault(order.Id)),
          PaidRevenue = Math.Max(0m, directCapturedByOrder.GetValueOrDefault(order.Id) - directRefundedByOrder.GetValueOrDefault(order.Id))
        })
        .ToList();

    var recognizedContractRows = recognizedContracts
        .Select(contract => new
        {
          VnTime = ToVnTime(contract.CreatedAt),
          NetRevenue = Math.Max(0m, (contract.TotalValue ?? 0m) - recognizedContractRefundById.GetValueOrDefault(contract.Id)),
          PaidRevenue = Math.Max(0m, contractPaidById.GetValueOrDefault(contract.Id) - recognizedContractRefundById.GetValueOrDefault(contract.Id))
        })
        .ToList();

    var dailyTrend = Enumerable.Range(0, 14)
        .Select(offset => todayVn.AddDays(offset - 13))
        .Select(day =>
        {
          var dayOrders = recognizedOrderRows.Where(item => item.VnTime.Date == day).ToList();
          var dayContracts = recognizedContractRows.Where(item => item.VnTime.Date == day).ToList();
          return new SupplierRevenueTrendPointResponse(
              day.ToString("yyyy-MM-dd"),
              day.ToString("dd/MM"),
              dayOrders.Sum(item => item.NetRevenue),
              dayContracts.Sum(item => item.NetRevenue),
              dayOrders.Sum(item => item.PaidRevenue) + dayContracts.Sum(item => item.PaidRevenue),
              dayOrders.Count,
              dayContracts.Count);
        })
        .ToList();

    var currentMonthStart = new DateTime(todayVn.Year, todayVn.Month, 1);
    var monthlyTrend = Enumerable.Range(0, 6)
        .Select(offset => currentMonthStart.AddMonths(offset - 5))
        .Select(monthStart =>
        {
          var monthOrders = recognizedOrderRows
              .Where(item => item.VnTime.Year == monthStart.Year && item.VnTime.Month == monthStart.Month)
              .ToList();
          var monthContracts = recognizedContractRows
              .Where(item => item.VnTime.Year == monthStart.Year && item.VnTime.Month == monthStart.Month)
              .ToList();
          return new SupplierRevenueTrendPointResponse(
              monthStart.ToString("yyyy-MM"),
              $"T{monthStart:MM/yy}",
              monthOrders.Sum(item => item.NetRevenue),
              monthContracts.Sum(item => item.NetRevenue),
              monthOrders.Sum(item => item.PaidRevenue) + monthContracts.Sum(item => item.PaidRevenue),
              monthOrders.Count,
              monthContracts.Count);
        })
        .ToList();

    var totalOrders = orderSummaries.Count;
    var pendingOrders = orderSummaries.Count(order => order.Status == OrderStatus.Pending);
    var processingOrders = orderSummaries.Count(order => order.Status is OrderStatus.Confirmed
        or OrderStatus.AwaitingCustomerDeliveryApproval
        or OrderStatus.Processing
        or OrderStatus.Shipped);
    var deliveredOrders = orderSummaries.Count(order => order.Status == OrderStatus.Delivered);
    var cancelledOrders = orderSummaries.Count(order => order.Status is OrderStatus.Cancelled or OrderStatus.Expired);
    var completedContractsCount = recognizedContracts.Count(contract =>
        contract.Status is ContractStatus.CompletedOnTime or ContractStatus.CompletedLate);

    return new SupplierOrderDashboardSummaryResponse(
        totalOrders,
        pendingOrders,
        processingOrders,
        packagingCount,
        directOrderRevenue + contractRevenue,
        directOrderRevenue,
        directOrderPaidRevenue,
        recognizedOrderIds.Count,
        contractRevenue,
        contractPaidRevenue,
        recognizedContracts.Count,
        completedContractsCount,
        totalOrderRefunded + allContractRefunds,
        deliveredOrders,
        cancelledOrders,
        dailyTrend,
        monthlyTrend);
  }

  public async Task<OrderPaymentSummaryResponse> GetPaymentSummaryAsync(Order order, CancellationToken cancellationToken = default)
  {
    var packaging = await context.CustomPackagings.AsNoTracking()
        .FirstOrDefaultAsync(item => item.OrderId == order.Id && !item.IsDeleted, cancellationToken);
    var packagingSummary = packaging is null
        ? null
        : new CustomPackagingSummaryResponse(packaging.Id, packaging.CompanyName ?? string.Empty,
            packaging.CompanyLogoUrl, packaging.BoxDesignUrl, packaging.PackagingType ?? string.Empty, packaging.Status, 0);

    var obligations = await context.OrderPaymentObligations.AsNoTracking()
        .Where(item => item.OrderId == order.Id && !item.IsDeleted)
        .OrderBy(item => item.SequenceNo)
        .ToListAsync(cancellationToken);
    var parts = await context.OrderPaymentParts.AsNoTracking()
        .Where(item => item.OrderId == order.Id && !item.IsDeleted)
        .OrderBy(item => item.PartIndex)
        .ToListAsync(cancellationToken);
    var payments = await context.Payments.AsNoTracking()
        .Where(item => item.OrderId == order.Id && !item.IsDeleted)
        .OrderByDescending(item => item.CreatedAt)
        .ToListAsync(cancellationToken);

    var partsByObligation = parts.GroupBy(x => x.ObligationId).ToDictionary(g => g.Key, g => g.ToList());
    return BuildPaymentSummary(order, packagingSummary, obligations, partsByObligation, payments);
  }

  public async Task<OrderFinancialSummaryResponse> GetFinancialSummaryAsync(Order order, CancellationToken cancellationToken = default)
  {
    var ledger = await context.OrderFinancialLedgerEntries.AsNoTracking()
        .Where(item => item.OrderId == order.Id && !item.IsDeleted)
        .OrderByDescending(item => item.CreatedAt)
        .ToListAsync(cancellationToken);
    var refunds = await context.OrderRefundCases.AsNoTracking()
        .Where(item => item.OrderId == order.Id && !item.IsDeleted)
        .OrderByDescending(item => item.CreatedAt)
        .ToListAsync(cancellationToken);
    var payouts = await context.SupplierPayoutRequests.AsNoTracking()
        .Where(item => item.OrderId == order.Id && !item.IsDeleted)
        .OrderByDescending(item => item.CreatedAt)
        .ToListAsync(cancellationToken);
    return BuildFinancialSummary(order, ledger, refunds, payouts);
  }

  public async Task<PagedResult<OrderFinancialLedgerItemResponse>> GetOrderLedgerPageAsync(Guid orderId,
      OrderLedgerPageQueryRequest query, CancellationToken cancellationToken = default)
  {
    IQueryable<OrderFinancialLedgerEntry> rows = context.OrderFinancialLedgerEntries.AsNoTracking()
        .Where(entry => entry.OrderId == orderId && !entry.IsDeleted);
    if (!string.IsNullOrWhiteSpace(query.Bucket)) rows = rows.Where(entry => entry.BalanceBucket == query.Bucket);
    if (!string.IsNullOrWhiteSpace(query.EntryType)) rows = rows.Where(entry => entry.EntryType == query.EntryType);
    if (query.From.HasValue) rows = rows.Where(entry => entry.CreatedAt >= query.From.Value);
    if (query.To.HasValue) rows = rows.Where(entry => entry.CreatedAt <= query.To.Value);
    var total = await rows.CountAsync(cancellationToken);
    rows = query.Sort == "createdAtAsc"
        ? rows.OrderBy(entry => entry.CreatedAt).ThenBy(entry => entry.Id)
        : rows.OrderByDescending(entry => entry.CreatedAt).ThenByDescending(entry => entry.Id);
    var items = await PagedQueryHelper.ApplyPaging(rows, query)
        .Select(entry => new OrderFinancialLedgerItemResponse(entry.Id, entry.EntryType,
            entry.BalanceBucket, entry.Amount, entry.Description, entry.CreatedAt))
        .ToListAsync(cancellationToken);
    return new PagedResult<OrderFinancialLedgerItemResponse>(items, query.Page, query.PageSize, total);
  }

  public async Task<(Payment? Payment, OrderPaymentPart? Part)> PreparePartVnPayPaymentAsync(
      Guid customerId, Guid orderId, Guid partId, CancellationToken cancellationToken = default)
  {
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    var order = await context.Orders.FromSqlInterpolated(
            $"SELECT * FROM orders WHERE id = {orderId} FOR UPDATE")
        .SingleOrDefaultAsync(cancellationToken);
    if (order is null || order.IsDeleted || order.CustomerId != customerId)
      return (null, null);
    if (order.Status is not (OrderStatus.Confirmed or OrderStatus.Processing or OrderStatus.CancellationRequested))
      return (null, null);

    var part = await context.OrderPaymentParts.FromSqlInterpolated(
            $"SELECT * FROM payment_parts WHERE id = {partId} FOR UPDATE")
        .SingleOrDefaultAsync(cancellationToken);
    if (part is null || part.IsDeleted || part.OrderId != orderId)
      return (null, null);
    if (part.Status is "Cancelled" or "Refunded")
      return (null, part);

    var existingPayment = await context.Payments.FromSqlInterpolated(
            $"SELECT * FROM payments WHERE part_id = {partId} AND is_deleted = FALSE FOR UPDATE")
        .OrderByDescending(item => item.CreatedAt)
        .FirstOrDefaultAsync(cancellationToken);

    var now = clock.UtcNow;
    if (part.Status == "Paid" || existingPayment?.Status == PaymentStatus.Completed)
    {
      await transaction.CommitAsync(cancellationToken);
      return (existingPayment, part);
    }

    if (existingPayment is null || existingPayment.Status == PaymentStatus.Failed)
    {
      var newPayment = new Payment
      {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        PartId = partId,
        CustomerId = customerId,
        PaymentNumber = $"PAY-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant(),
        Amount = part.Amount,
        Currency = "VND",
        Method = PaymentMethod.VNPay,
        Provider = "VNPay",
        Status = PaymentStatus.Pending,
        ExpiresAt = now.AddHours(24),
        CreatedBy = customerId,
        CreatedAt = now,
        UpdatedAt = now
      };
      context.Payments.Add(newPayment);
      part.Status = "Processing";
      part.UpdatedAt = now;
      await context.SaveChangesAsync(cancellationToken);
      await transaction.CommitAsync(cancellationToken);
      return (newPayment, part);
    }

    existingPayment.ExpiresAt = now.AddHours(24);
    existingPayment.UpdatedAt = now;
    existingPayment.UpdatedBy = customerId;
    part.Status = "Processing";
    part.UpdatedAt = now;
    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return (existingPayment, part);
  }

  public async Task<(OrderCancellationRequestResponse Request, OrderStatus UpdatedOrderStatus)> SubmitCancellationRequestAsync(
      Order order, Guid actorUserId, bool isSupplier, CreateOrderCancellationRequest request,
      CancellationToken cancellationToken = default)
  {
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    var lockedOrder = await context.Orders.FromSqlInterpolated(
            $"SELECT * FROM orders WHERE id = {order.Id} FOR UPDATE")
        .SingleOrDefaultAsync(cancellationToken);
    if (lockedOrder is null || lockedOrder.IsDeleted)
      throw new HttpException(404, B2BOrderingMessages.OrderNotFound);
    if (request.ExpectedVersion.HasValue && lockedOrder.Version != request.ExpectedVersion.Value)
      throw new HttpException(409, "Order version changed concurrently. Refresh and try again.", OrderErrorCodes.StateConflict);

    // Matrix 10.10: Shipped / Delivered / Cancelled / Expired cannot submit cancellation requests
    if (lockedOrder.Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled or OrderStatus.Expired)
      throw new HttpException(409, $"Orders in status '{lockedOrder.Status}' cannot be cancelled.", OrderErrorCodes.StateConflict);

    var hasOpenCancel = await context.OrderCancellationRequests
        .AnyAsync(item => item.OrderId == lockedOrder.Id && item.Status == "Pending" && !item.IsDeleted, cancellationToken);
    if (hasOpenCancel)
      throw new HttpException(409, "A cancellation request is already pending for this order.", OrderErrorCodes.StateConflict);

    var paidAmount = await context.Payments
        .Where(item => item.OrderId == lockedOrder.Id && item.Status == PaymentStatus.Completed && !item.IsDeleted)
        .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0m;

    var now = clock.UtcNow;
    var reason = request.Reason.Trim();
    var supplierUserId = await GetSupplierUserIdByShopAsync(lockedOrder.ShopId, cancellationToken);

    // D24: default refund suggestion (50% for Processing with approved/in-production custom packaging, else 100%).
    var packagingStatusForSuggestion = await context.CustomPackagings.AsNoTracking()
        .Where(item => item.OrderId == lockedOrder.Id && !item.IsDeleted)
        .Select(item => (CustomPackagingStatus?)item.Status)
        .FirstOrDefaultAsync(cancellationToken);
    var suggestion = OrderRefundSuggestionHelper.Suggest(lockedOrder.Status, packagingStatusForSuggestion, paidAmount);

    // Matrix 10.10:
    // 1) Pending or AwaitingCustomerDeliveryApproval, OR Confirmed with 0 paid and Customer/Supplier cancels before production:
    //    Wait: Confirmed with 0 paid -> unilateral immediate cancel allowed!
    //    Confirmed with paidAmount > 0 -> Supplier unilateral cancel auto-approves + opens 100% refund case; Customer cancel requires Supplier approval (CancellationRequested)!
    //    Processing -> mutual approval required (CancellationRequested)!
    var autoApprove = lockedOrder.Status is OrderStatus.Pending or OrderStatus.AwaitingCustomerDeliveryApproval
        || (lockedOrder.Status == OrderStatus.Confirmed && (paidAmount == 0m || isSupplier));

    var cancelEntity = new OrderCancellationRequest
    {
      Id = Guid.NewGuid(),
      OrderId = lockedOrder.Id,
      RequestedByUserId = actorUserId,
      RequestedByRole = isSupplier ? "Supplier" : "Customer",
      OrderStatusSnapshot = lockedOrder.Status.ToString(),
      Reason = reason,
      Status = autoApprove ? "AutoApproved" : "Pending",
      ProposedRefundAmount = suggestion.Amount,
      ReviewedByUserId = autoApprove ? actorUserId : null,
      ReviewedAt = autoApprove ? now : null,
      ReviewNotes = autoApprove ? "Automatically approved under Direct Bulk Order policy matrix." : null,
      CreatedBy = actorUserId,
      CreatedAt = now,
      UpdatedAt = now
    };
    context.OrderCancellationRequests.Add(cancelEntity);

    if (autoApprove)
    {
      await ExecuteOrderCancellationCoreAsync(lockedOrder, cancelEntity.Id, actorUserId, reason, paidAmount, now, cancellationToken, suggestion.Amount);
    }
    else
    {
      var previousStatus = lockedOrder.Status;
      lockedOrder.Status = OrderStatus.CancellationRequested;
      lockedOrder.Version += 1;
      lockedOrder.UpdatedAt = now;
      lockedOrder.UpdatedBy = actorUserId;

      context.WorkflowEvents.Add(new WorkflowEvent
      {
        AggregateType = OrderAggregateType,
        AggregateId = lockedOrder.Id,
        EventType = "OrderCancellationRequested",
        FromStatus = previousStatus.ToString(),
        ToStatus = OrderStatus.CancellationRequested.ToString(),
        ActorUserId = actorUserId,
        Reason = reason
      });

      var notifyTarget = isSupplier ? lockedOrder.CustomerId : supplierUserId;
      if (notifyTarget != Guid.Empty)
      {
        context.Notifications.Add(new Notification
        {
          UserId = notifyTarget,
          Type = "order.cancellation_requested",
          Title = "Cancellation request received",
          Content = $"A cancellation request was submitted for order {lockedOrder.OrderNumber}.",
          ReferenceType = OrderReferenceType,
          ReferenceId = lockedOrder.Id
        });
      }
    }

    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return (MapCancelRequest(cancelEntity) with
    {
      SuggestedRefundAmount = suggestion.Amount,
      SuggestedRefundPercent = suggestion.Percent,
      SuggestionReason = suggestion.Reason
    }, lockedOrder.Status);
  }

  public async Task<(OrderCancellationRequestResponse Request, OrderStatus UpdatedOrderStatus)> RespondCancellationRequestAsync(
      Order order, Guid requestId, Guid actorUserId, bool isSupplier, bool isAdmin,
      RespondOrderCancellationRequest request, CancellationToken cancellationToken = default)
  {
    context.ChangeTracker.Clear();
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    var lockedOrder = await context.Orders.FromSqlInterpolated(
            $"SELECT * FROM orders WHERE id = {order.Id} FOR UPDATE")
        .SingleOrDefaultAsync(cancellationToken);
    var cancelReq = await context.OrderCancellationRequests.FromSqlInterpolated(
            $"SELECT * FROM order_cancellation_requests WHERE id = {requestId} FOR UPDATE")
        .SingleOrDefaultAsync(cancellationToken);
    if (lockedOrder is null || cancelReq is null || cancelReq.OrderId != lockedOrder.Id || cancelReq.Status != "Pending")
      throw new HttpException(409, "No pending cancellation request was found for this order.", OrderErrorCodes.StateConflict);

    if (!isAdmin)
    {
      var requestedBySupplier = string.Equals(cancelReq.RequestedByRole, "Supplier", StringComparison.OrdinalIgnoreCase);
      if (requestedBySupplier && isSupplier)
        throw new HttpException(403, "Only the customer or an administrator can respond to a supplier cancellation request.", OrderErrorCodes.TenantForbidden);
      if (!requestedBySupplier && !isSupplier)
        throw new HttpException(403, "Only the supplier or an administrator can respond to a customer cancellation request.", OrderErrorCodes.TenantForbidden);
    }

    var now = clock.UtcNow;
    cancelReq.ReviewedByUserId = actorUserId;
    cancelReq.ReviewedAt = now;
    cancelReq.ReviewNotes = request.Notes?.Trim();
    cancelReq.UpdatedBy = actorUserId;
    cancelReq.UpdatedAt = now;

    if (request.Approve)
    {
      cancelReq.Status = "Approved";
      var paidAmount = await context.Payments
          .Where(item => item.OrderId == lockedOrder.Id && item.Status == PaymentStatus.Completed && !item.IsDeleted)
          .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0m;
      await ExecuteOrderCancellationCoreAsync(lockedOrder, cancelReq.Id, actorUserId, cancelReq.Reason, paidAmount, now, cancellationToken,
          Math.Min(paidAmount, cancelReq.ProposedRefundAmount));
    }
    else
    {
      cancelReq.Status = "Rejected";
      var restoredStatus = Enum.TryParse<OrderStatus>(cancelReq.OrderStatusSnapshot, out var parsed)
          ? parsed
          : OrderStatus.Confirmed;
      lockedOrder.Status = restoredStatus;
      lockedOrder.Version += 1;
      lockedOrder.UpdatedAt = now;
      lockedOrder.UpdatedBy = actorUserId;

      context.WorkflowEvents.Add(new WorkflowEvent
      {
        AggregateType = OrderAggregateType,
        AggregateId = lockedOrder.Id,
        EventType = "OrderCancellationRejected",
        FromStatus = OrderStatus.CancellationRequested.ToString(),
        ToStatus = restoredStatus.ToString(),
        ActorUserId = actorUserId,
        Reason = cancelReq.ReviewNotes
      });
      context.Notifications.Add(new Notification
      {
        UserId = cancelReq.RequestedByUserId,
        Type = "order.cancellation_rejected",
        Title = "Cancellation request declined",
        Content = $"The cancellation request for order {lockedOrder.OrderNumber} was declined.",
        ReferenceType = OrderReferenceType,
        ReferenceId = lockedOrder.Id
      });
    }

    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return (MapCancelRequest(cancelReq), lockedOrder.Status);
  }

  public async Task<bool> ConfirmOrderDeliveryWithProofAsync(
      Order order, Guid customerId, Guid? deliveryId, string? notes, CancellationToken cancellationToken = default)
  {
    context.ChangeTracker.Clear();
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    var lockedOrder = context.Database.IsRelational()
        ? await context.Orders.FromSqlInterpolated(
                $"SELECT * FROM orders WHERE id = {order.Id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
        : await context.Orders.SingleOrDefaultAsync(o => o.Id == order.Id, cancellationToken);
    if (lockedOrder is null || lockedOrder.IsDeleted || lockedOrder.CustomerId != customerId || lockedOrder.Status != OrderStatus.Shipped)
      return false;

    var deliveryQuery = context.Deliveries.Where(item => item.OrderId == lockedOrder.Id && !item.IsDeleted);
    if (deliveryId.HasValue)
      deliveryQuery = deliveryQuery.Where(item => item.Id == deliveryId.Value);

    var delivery = await deliveryQuery.OrderByDescending(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);
    if (delivery is null)
      throw new HttpException(409, "Cannot confirm receipt before the supplier creates a shipment and uploads Proof-of-Delivery photos.", OrderErrorCodes.DeliveryProofRequired);

    var hasDeliveryProof = await context.DeliveryProofs.AsNoTracking()
        .AnyAsync(item => item.DeliveryId == delivery.Id && item.ProofType == ProofType.Delivery && !item.IsDeleted, cancellationToken);
    if (!hasDeliveryProof)
      throw new HttpException(409, "Cannot confirm receipt before Proof-of-Delivery photos are uploaded.", OrderErrorCodes.DeliveryProofRequired);

    await ExecuteOrderDeliveryConfirmationCoreAsync(
        lockedOrder, delivery, customerId, notes, isAutoConfirmed: false, cancellationToken);

    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    order.Status = OrderStatus.Delivered;
    order.Version = lockedOrder.Version;
    return true;
  }

  private async Task RewardSupplierReputationOnDeliveryAsync(
      Order lockedOrder, DateTime now, Guid actorUserId, CancellationToken cancellationToken)
  {
    var alreadyRewarded = await context.ReputationLogs
        .AnyAsync(l => l.ReferenceId == lockedOrder.Id && l.ReferenceType == "Order" && !l.IsDeleted, cancellationToken);
    if (alreadyRewarded)
      return;

    var shop = await context.Shops.FirstOrDefaultAsync(s => s.Id == lockedOrder.ShopId && !s.IsDeleted, cancellationToken);
    if (shop is null)
      return;

    var supplier = await context.SupplierProfiles.FirstOrDefaultAsync(s => s.Id == shop.SupplierId && !s.IsDeleted, cancellationToken);
    if (supplier is null)
      return;

    supplier.ReputationScore = Math.Max(0m, supplier.ReputationScore + 5m);
    supplier.UpdatedAt = now;
    supplier.UpdatedBy = actorUserId;

    context.ReputationLogs.Add(new ReputationLog
    {
      Id = Guid.NewGuid(),
      SupplierId = supplier.Id,
      EventType = ReputationEvent.CompletedOnTime,
      ScoreDelta = 5m,
      Reason = $"Direct order {lockedOrder.OrderNumber} delivered successfully.",
      ReferenceId = lockedOrder.Id,
      ReferenceType = "Order",
      CreatedAt = now,
      CreatedBy = actorUserId
    });
  }

  private async Task ExecuteOrderDeliveryConfirmationCoreAsync(
      Order lockedOrder, Delivery delivery, Guid actorUserId, string? notes, bool isAutoConfirmed, CancellationToken cancellationToken)
  {
    var now = clock.UtcNow;
    delivery.Status = DeliveryStatus.Delivered;
    delivery.ActualDeliveryDate = DateOnly.FromDateTime(now);
    delivery.UpdatedBy = actorUserId;
    delivery.UpdatedAt = now;

    lockedOrder.Status = OrderStatus.Delivered;
    lockedOrder.Version += 1;
    lockedOrder.UpdatedBy = actorUserId;
    lockedOrder.UpdatedAt = now;

    await context.CustomPackagings
        .Where(item => item.OrderId == lockedOrder.Id && item.Status == CustomPackagingStatus.InProduction && !item.IsDeleted)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.Status, CustomPackagingStatus.Completed)
            .SetProperty(item => item.UpdatedAt, now)
            .SetProperty(item => item.UpdatedBy, actorUserId), cancellationToken);

    // Unlock Held -> Eligible in simulated financial ledger!
    await UnlockHeldBalanceToEligibleCoreAsync(lockedOrder, now, actorUserId, cancellationToken);

    // D26: Award +5 reputation to supplier idempotently
    await RewardSupplierReputationOnDeliveryAsync(lockedOrder, now, actorUserId, cancellationToken);

    var supplierUserId = await GetSupplierUserIdByShopAsync(lockedOrder.ShopId, cancellationToken);
    context.WorkflowEvents.Add(new WorkflowEvent
    {
      AggregateType = OrderAggregateType,
      AggregateId = lockedOrder.Id,
      EventType = "OrderDeliveryConfirmed",
      FromStatus = OrderStatus.Shipped.ToString(),
      ToStatus = OrderStatus.Delivered.ToString(),
      ActorUserId = actorUserId,
      Reason = notes
    });

    if (isAutoConfirmed)
    {
      if (supplierUserId != Guid.Empty)
      {
        context.Notifications.Add(new Notification
        {
          UserId = supplierUserId,
          Type = "order.delivered",
          Title = "Order delivery auto-confirmed",
          Content = $"Order {lockedOrder.OrderNumber} was automatically confirmed delivered after 72 hours. Funds are now eligible for simulated payout.",
          ReferenceType = OrderReferenceType,
          ReferenceId = lockedOrder.Id
        });
      }
      if (lockedOrder.CustomerId != Guid.Empty)
      {
        context.Notifications.Add(new Notification
        {
          UserId = lockedOrder.CustomerId,
          Type = "order.delivered",
          Title = "Order auto-confirmed",
          Content = $"Order {lockedOrder.OrderNumber} was automatically confirmed delivered after 72 hours.",
          ReferenceType = OrderReferenceType,
          ReferenceId = lockedOrder.Id
        });
      }
    }
    else
    {
      if (supplierUserId != Guid.Empty)
      {
        context.Notifications.Add(new Notification
        {
          UserId = supplierUserId,
          Type = "order.delivered",
          Title = "Customer confirmed order delivery",
          Content = $"Order {lockedOrder.OrderNumber} was confirmed delivered. Funds are now eligible for simulated payout.",
          ReferenceType = OrderReferenceType,
          ReferenceId = lockedOrder.Id
        });
      }
    }
  }

  public async Task<OrderDeliveryDisputeResponse> ReportDeliveryDisputeAsync(
      Order order, Guid customerId, ReportOrderDeliveryDisputeRequest request, CancellationToken cancellationToken = default)
  {
    context.ChangeTracker.Clear();
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    var lockedOrder = await context.Orders.FromSqlInterpolated(
            $"SELECT * FROM orders WHERE id = {order.Id} FOR UPDATE")
        .SingleOrDefaultAsync(cancellationToken);
    if (lockedOrder is null || lockedOrder.IsDeleted || lockedOrder.CustomerId != customerId)
      throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);
    if (lockedOrder.Status is not (OrderStatus.Shipped or OrderStatus.Delivered))
      throw new HttpException(409, "Delivery issues can only be reported after the order has been shipped.", OrderErrorCodes.StateConflict);

    var existingOpen = await context.OrderDeliveryDisputes
        .AnyAsync(item => item.OrderId == lockedOrder.Id
            && (item.Status == "Open" || item.Status == "AwaitingSupplierRebuttal")
            && !item.IsDeleted, cancellationToken);
    if (existingOpen)
      throw new HttpException(409, "An open delivery dispute already exists for this order.", OrderErrorCodes.StateConflict);

    var delivery = await context.Deliveries
        .Where(item => item.OrderId == lockedOrder.Id && !item.IsDeleted)
        .OrderByDescending(item => item.CreatedAt)
        .FirstOrDefaultAsync(cancellationToken);

    var now = clock.UtcNow;
    if (delivery is not null)
    {
      delivery.Status = DeliveryStatus.Disputed;
      delivery.UpdatedAt = now;
      delivery.UpdatedBy = customerId;
    }

    var previousStatus = lockedOrder.Status;
    lockedOrder.Status = OrderStatus.Disputed;
    lockedOrder.Version += 1;
    lockedOrder.UpdatedAt = now;
    lockedOrder.UpdatedBy = customerId;

    var dispute = new OrderDeliveryDispute
    {
      Id = Guid.NewGuid(),
      OrderId = lockedOrder.Id,
      DeliveryId = request.DeliveryId ?? delivery?.Id,
      CustomerId = customerId,
      ShopId = lockedOrder.ShopId,
      IssueType = request.IssueType.Trim(),
      Description = request.Description.Trim(),
      EvidenceUrlsJson = request.EvidenceUrls is { Count: > 0 } ? JsonSerializer.Serialize(request.EvidenceUrls) : null,
      RefundBankSnapshotJson = SerializeCustomerBankSnapshot(request.RefundBankName, request.RefundAccountNumber, request.RefundAccountHolderName),
      Status = "AwaitingSupplierRebuttal",
      RebuttalDeadlineAt = now.AddHours(48),
      CreatedBy = customerId,
      CreatedAt = now,
      UpdatedAt = now
    };
    context.OrderDeliveryDisputes.Add(dispute);

    // Lock Held/Eligible funds into Disputed bucket!
    var currentNetHeldOrEligible = await GetUnlockableNetBalanceAsync(lockedOrder.Id, cancellationToken);
    if (currentNetHeldOrEligible > 0m)
    {
      AddLedgerEntryIfMissing(new OrderFinancialLedgerEntry
      {
        Id = Guid.NewGuid(),
        OrderId = lockedOrder.Id,
        ShopId = lockedOrder.ShopId,
        CustomerId = lockedOrder.CustomerId,
        EntryType = "DisputedLock",
        BalanceBucket = "Disputed",
        Amount = currentNetHeldOrEligible,
        Description = $"Order funds locked due to delivery dispute ({dispute.IssueType}).",
        IdempotencyKey = $"dispute-lock:{dispute.Id}",
        CreatedBy = customerId,
        CreatedAt = now,
        UpdatedAt = now
      });
    }

    context.WorkflowEvents.Add(new WorkflowEvent
    {
      AggregateType = OrderAggregateType,
      AggregateId = lockedOrder.Id,
      EventType = "OrderDeliveryDisputed",
      FromStatus = previousStatus.ToString(),
      ToStatus = OrderStatus.Disputed.ToString(),
      ActorUserId = customerId,
      Reason = dispute.Description
    });

    var supplierUserId = await GetSupplierUserIdByShopAsync(lockedOrder.ShopId, cancellationToken);
    if (supplierUserId != Guid.Empty)
    {
      context.Notifications.Add(new Notification
      {
        UserId = supplierUserId,
        Type = "order.delivery_disputed",
        Title = "Delivery issue reported",
        Content = $"Customer reported a delivery issue on order {lockedOrder.OrderNumber}. Payout is locked pending review. You have 48 hours to submit a rebuttal before admin review.",
        ReferenceType = OrderReferenceType,
        ReferenceId = lockedOrder.Id
      });
    }

    var paidAmount = await GetOrderPaidBalanceAsync(lockedOrder.Id, cancellationToken);
    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return MapDispute(dispute, lockedOrder.OrderNumber, paidAmount);
  }

  public async Task<OrderDeliveryDisputeResponse> ResolveDeliveryDisputeAsync(
      Guid disputeId, Guid adminUserId, ResolveOrderDeliveryDisputeRequest request, CancellationToken cancellationToken = default)
  {
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    var dispute = await (context.Database.IsRelational()
        ? context.OrderDeliveryDisputes.FromSqlInterpolated(
            $"SELECT * FROM order_delivery_disputes WHERE id = {disputeId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
        : context.OrderDeliveryDisputes.SingleOrDefaultAsync(d => d.Id == disputeId, cancellationToken))
        ?? throw new HttpException(404, "Delivery dispute was not found.");

    if (dispute.Status is not ("Open" or "AwaitingSupplierRebuttal"))
      throw new HttpException(409, "Delivery dispute has already been resolved.", OrderErrorCodes.StateConflict);

    var now = clock.UtcNow;
    var hasRebuttal = dispute.SupplierRebuttalAt.HasValue || dispute.Status == "Open";
    var isDeadlinePassed = dispute.RebuttalDeadlineAt.HasValue && now >= dispute.RebuttalDeadlineAt.Value;

    if (!hasRebuttal && !isDeadlinePassed)
      throw new HttpException(409, "Cannot resolve delivery dispute while awaiting supplier rebuttal within the 48-hour window.", OrderErrorCodes.StateConflict);

    var action = request.ResolutionAction.Trim();
    var isFullRefund = string.Equals(action, "FullRefund", StringComparison.OrdinalIgnoreCase)
        || string.Equals(action, "Refund", StringComparison.OrdinalIgnoreCase);
    var isPartialRefund = string.Equals(action, "PartialRefund", StringComparison.OrdinalIgnoreCase);
    var isReleasePayout = string.Equals(action, "ReleasePayout", StringComparison.OrdinalIgnoreCase);
    if (!isFullRefund && !isPartialRefund && !isReleasePayout)
      throw new HttpException(400, "Invalid dispute resolution action. Use ReleasePayout, FullRefund, or PartialRefund.", OrderErrorCodes.ValidationError);

    var lockedOrder = await (context.Database.IsRelational()
        ? context.Orders.FromSqlInterpolated(
            $"SELECT * FROM orders WHERE id = {dispute.OrderId} FOR UPDATE")
            .SingleAsync(cancellationToken)
        : context.Orders.SingleAsync(o => o.Id == dispute.OrderId, cancellationToken));

    var refundableBalance = await GetOrderPaidBalanceAsync(lockedOrder.Id, cancellationToken);
    decimal refundAmount = 0m;
    if (isPartialRefund)
    {
      if (!request.RefundAmount.HasValue || request.RefundAmount.Value <= 0m)
        throw new HttpException(400, "Partial refund requires a positive refund amount.", OrderErrorCodes.ValidationError);
      if (request.RefundAmount.Value > refundableBalance)
        throw new HttpException(409, $"Refund amount cannot exceed refundable paid balance ({refundableBalance:N0} VND).", OrderErrorCodes.StateConflict);
      refundAmount = decimal.Round(request.RefundAmount.Value, 2, MidpointRounding.AwayFromZero);
    }
    else if (isFullRefund)
    {
      refundAmount = refundableBalance;
    }

    var delivery = dispute.DeliveryId.HasValue
        ? await context.Deliveries.FirstOrDefaultAsync(item => item.Id == dispute.DeliveryId.Value && !item.IsDeleted, cancellationToken)
        : await context.Deliveries.Where(item => item.OrderId == lockedOrder.Id && !item.IsDeleted)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    dispute.ResolutionNotes = request.Notes.Trim();
    dispute.ResolvedByAdminId = adminUserId;
    dispute.ResolvedAt = now;
    dispute.UpdatedBy = adminUserId;
    dispute.UpdatedAt = now;

    // Unlock disputed lock first so ledger reflects available Held balance after commit/save.
    var lockedEntry = await context.OrderFinancialLedgerEntries.AsNoTracking()
        .FirstOrDefaultAsync(item => item.IdempotencyKey == $"dispute-lock:{dispute.Id}", cancellationToken);
    if (lockedEntry is not null)
    {
      context.OrderFinancialLedgerEntries.Add(new OrderFinancialLedgerEntry
      {
        Id = Guid.NewGuid(),
        OrderId = lockedOrder.Id,
        ShopId = lockedOrder.ShopId,
        CustomerId = lockedOrder.CustomerId,
        EntryType = "DisputeUnlocked",
        BalanceBucket = "Disputed",
        Amount = -lockedEntry.Amount,
        Description = $"Dispute resolved by administrator ({action}).",
        IdempotencyKey = $"dispute-unlock:{dispute.Id}",
        CreatedBy = adminUserId,
        CreatedAt = now,
        UpdatedAt = now
      });
    }

    if (isFullRefund || isPartialRefund)
    {
      dispute.Status = "ResolvedRefund";

      if (refundAmount > 0m)
      {
        var hasTransferProofOrRef = !string.IsNullOrWhiteSpace(request.TransferProofImageUrl)
            || !string.IsNullOrWhiteSpace(request.RefundReferenceCode);
        var completeNow = !request.DeferTransfer && hasTransferProofOrRef;

        var refundCase = new OrderRefundCase
        {
          Id = Guid.NewGuid(),
          OrderId = lockedOrder.Id,
          CustomerId = lockedOrder.CustomerId,
          ShopId = lockedOrder.ShopId,
          CaseNumber = $"RFD-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
          RequestedAmount = refundAmount,
          ApprovedAmount = completeNow ? refundAmount : 0m,
          Reason = $"Dispute resolution refund ({action}): {dispute.ResolutionNotes}",
          Status = completeNow ? "CompletedSimulated" : "Pending",
          SandboxSimulated = completeNow && string.IsNullOrWhiteSpace(request.TransferProofImageUrl),
          CustomerBankSnapshotJson = dispute.RefundBankSnapshotJson,
          TransferProofImageUrl = completeNow && !string.IsNullOrWhiteSpace(request.TransferProofImageUrl)
                ? request.TransferProofImageUrl.Trim()
                : null,
          ProcessedByAdminId = completeNow ? adminUserId : null,
          ProcessedAt = completeNow ? now : null,
          AdminNotes = completeNow ? dispute.ResolutionNotes : null,
          RefundReferenceCode = completeNow
                ? (string.IsNullOrWhiteSpace(request.RefundReferenceCode)
                    ? $"TRF-RFD-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant()
                    : request.RefundReferenceCode.Trim())
                : null,
          CreatedBy = adminUserId,
          CreatedAt = now,
          UpdatedAt = now
        };
        context.OrderRefundCases.Add(refundCase);

        if (completeNow)
        {
          await ApplyCompletedRefundCoreAsync(
              lockedOrder,
              refundCase,
              refundAmount,
              adminUserId,
              now,
              $"dispute-refund:{dispute.Id}",
              $"Refund completed from dispute {dispute.Id} ({refundCase.RefundReferenceCode}).",
              cancellationToken);
        }
        else
        {
          context.OrderFinancialLedgerEntries.Add(new OrderFinancialLedgerEntry
          {
            Id = Guid.NewGuid(),
            OrderId = lockedOrder.Id,
            ShopId = lockedOrder.ShopId,
            CustomerId = lockedOrder.CustomerId,
            RefundCaseId = refundCase.Id,
            EntryType = "ReservedForRefund",
            BalanceBucket = "Held",
            Amount = 0m,
            Description = $"Refund case {refundCase.CaseNumber} approved ({refundAmount:N0} VND) awaiting bank transfer proof.",
            IdempotencyKey = $"dispute-refund-pending:{dispute.Id}",
            CreatedBy = adminUserId,
            CreatedAt = now,
            UpdatedAt = now
          });
          context.Notifications.Add(new Notification
          {
            UserId = lockedOrder.CustomerId,
            Type = "order.refund_approved",
            Title = "Dispute approved for refund",
            Content = $"Dispute for order {lockedOrder.OrderNumber} was resolved in your favor ({refundAmount:N0} VND). Refund case {refundCase.CaseNumber} is queued for bank transfer.",
            ReferenceType = OrderReferenceType,
            ReferenceId = lockedOrder.Id
          });
        }
      }

      if (refundAmount >= refundableBalance)
      {
        lockedOrder.Status = OrderStatus.Cancelled;
        lockedOrder.CancelledAt = now;
        lockedOrder.CancelledByUserId = adminUserId;
        lockedOrder.CancellationReason = dispute.ResolutionNotes;
        if (delivery is not null && delivery.Status == DeliveryStatus.Disputed)
        {
          delivery.Status = DeliveryStatus.Returned;
          delivery.UpdatedAt = now;
          delivery.UpdatedBy = adminUserId;
        }
        await CancelOpenPaymentScheduleCoreAsync(lockedOrder.Id, now, cancellationToken);
      }
      else
      {
        lockedOrder.Status = OrderStatus.Delivered;
        if (delivery is not null && delivery.Status == DeliveryStatus.Disputed)
        {
          delivery.Status = DeliveryStatus.Delivered;
          delivery.UpdatedAt = now;
          delivery.UpdatedBy = adminUserId;
        }
        await context.SaveChangesAsync(cancellationToken);
        await UnlockHeldBalanceToEligibleCoreAsync(lockedOrder, now, adminUserId, cancellationToken);
      }
    }
    else
    {
      dispute.Status = "ResolvedReleasePayout";
      lockedOrder.Status = OrderStatus.Delivered;
      if (delivery is not null && delivery.Status == DeliveryStatus.Disputed)
      {
        delivery.Status = DeliveryStatus.Delivered;
        delivery.UpdatedAt = now;
        delivery.UpdatedBy = adminUserId;
      }
      await context.SaveChangesAsync(cancellationToken);
      await UnlockHeldBalanceToEligibleCoreAsync(lockedOrder, now, adminUserId, cancellationToken);
    }

    lockedOrder.Version += 1;
    lockedOrder.UpdatedBy = adminUserId;
    lockedOrder.UpdatedAt = now;

    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    var updatedPaid = await GetOrderPaidBalanceAsync(lockedOrder.Id, cancellationToken);
    return MapDispute(dispute, lockedOrder.OrderNumber, updatedPaid);
  }

  public async Task<OrderDeliveryDisputeResponse> SubmitDeliveryDisputeRebuttalAsync(
      Guid supplierUserId,
      Guid orderId,
      Guid disputeId,
      SubmitSupplierDeliveryDisputeRebuttalRequest request,
      CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(request.Notes))
      throw new HttpException(400, "Rebuttal notes are required.", OrderErrorCodes.ValidationError);

    context.ChangeTracker.Clear();
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

    var dispute = await (context.Database.IsRelational()
        ? context.OrderDeliveryDisputes.FromSqlInterpolated(
            $"SELECT * FROM order_delivery_disputes WHERE id = {disputeId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
        : context.OrderDeliveryDisputes.SingleOrDefaultAsync(d => d.Id == disputeId, cancellationToken))
        ?? throw new HttpException(404, "Delivery dispute was not found.");

    if (dispute.OrderId != orderId || dispute.IsDeleted)
      throw new HttpException(404, "Delivery dispute was not found for this order.");

    var lockedOrder = await (context.Database.IsRelational()
        ? context.Orders.FromSqlInterpolated(
            $"SELECT * FROM orders WHERE id = {orderId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
        : context.Orders.SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken))
        ?? throw new HttpException(404, B2BOrderingMessages.OrderNotFound);

    var ownedShopIds = await GetOwnedShopIdsAsync(supplierUserId, cancellationToken);
    if (!ownedShopIds.Contains(lockedOrder.ShopId))
      throw new HttpException(403, "You do not have permission to respond to disputes for this shop.", OrderErrorCodes.TenantForbidden);

    if (dispute.Status != "AwaitingSupplierRebuttal")
      throw new HttpException(409, "Dispute is not awaiting supplier rebuttal.", OrderErrorCodes.StateConflict);

    var now = clock.UtcNow;
    if (dispute.RebuttalDeadlineAt.HasValue && now > dispute.RebuttalDeadlineAt.Value)
      throw new HttpException(409, "The 48-hour rebuttal window for this delivery dispute has expired.", OrderErrorCodes.StateConflict);

    dispute.Status = "Open";
    dispute.SupplierRebuttalNotes = request.Notes.Trim();
    dispute.SupplierRebuttalEvidenceUrlsJson = request.EvidenceUrls is { Count: > 0 }
        ? JsonSerializer.Serialize(request.EvidenceUrls)
        : null;
    dispute.SupplierRebuttalAt = now;
    dispute.UpdatedAt = now;
    dispute.UpdatedBy = supplierUserId;

    lockedOrder.Version += 1;
    lockedOrder.UpdatedAt = now;
    lockedOrder.UpdatedBy = supplierUserId;

    context.WorkflowEvents.Add(new WorkflowEvent
    {
      AggregateType = OrderAggregateType,
      AggregateId = lockedOrder.Id,
      EventType = "OrderDeliveryDisputeRebutted",
      FromStatus = "AwaitingSupplierRebuttal",
      ToStatus = "Open",
      ActorUserId = supplierUserId,
      Reason = dispute.SupplierRebuttalNotes
    });

    context.Notifications.Add(new Notification
    {
      UserId = lockedOrder.CustomerId,
      Type = "order.dispute_rebutted",
      Title = "Supplier responded to delivery dispute",
      Content = $"Supplier submitted a rebuttal for the delivery dispute on order {lockedOrder.OrderNumber}. Admin will review and resolve.",
      ReferenceType = OrderReferenceType,
      ReferenceId = lockedOrder.Id
    });

    var adminUserIds = await context.Users.AsNoTracking()
        .Where(u => u.Role == UserRole.Admin && u.IsActive && !u.IsDeleted)
        .Select(u => u.Id)
        .ToListAsync(cancellationToken);

    foreach (var adminId in adminUserIds)
    {
      context.Notifications.Add(new Notification
      {
        UserId = adminId,
        Type = "order.dispute_rebutted",
        Title = "Supplier submitted delivery dispute rebuttal",
        Content = $"Supplier submitted a rebuttal for dispute on order {lockedOrder.OrderNumber}. The dispute is now open for review.",
        ReferenceType = OrderReferenceType,
        ReferenceId = lockedOrder.Id
      });
    }

    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);

    var paidAmount = await GetOrderPaidBalanceAsync(lockedOrder.Id, cancellationToken);
    return MapDispute(dispute, lockedOrder.OrderNumber, paidAmount);
  }

  public async Task<OrderRefundCaseResponse> CreateRefundCaseAsync(
      Order order, Guid customerId, CreateOrderRefundRequest request, CancellationToken cancellationToken = default)
  {
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    var lockedOrder = await context.Orders.FromSqlInterpolated(
            $"SELECT * FROM orders WHERE id = {order.Id} FOR UPDATE")
        .SingleOrDefaultAsync(cancellationToken)
        ?? throw new HttpException(404, B2BOrderingMessages.OrderNotFound);
    if (lockedOrder.Status is not (OrderStatus.Cancelled or OrderStatus.Expired or OrderStatus.Disputed))
      throw new HttpException(409, "Refund claims are only available for cancelled, expired, or disputed orders.", OrderErrorCodes.StateConflict);

    var paidAmount = await context.Payments
        .Where(item => item.OrderId == lockedOrder.Id && item.Status == PaymentStatus.Completed && !item.IsDeleted)
        .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0m;
    var existingRefundedOrPending = await context.OrderRefundCases
        .Where(item => item.OrderId == lockedOrder.Id && item.Status != "Rejected" && !item.IsDeleted)
        .SumAsync(item => (decimal?)item.RequestedAmount, cancellationToken) ?? 0m;

    var refundable = Math.Max(0m, paidAmount - existingRefundedOrPending);
    if (refundable <= 0m)
      throw new HttpException(409, "No refundable paid balance remains on this order.", OrderErrorCodes.StateConflict);

    var requestedAmount = request.RequestedAmount.HasValue
        ? Math.Min(refundable, decimal.Round(request.RequestedAmount.Value, 2, MidpointRounding.AwayFromZero))
        : refundable;

    var now = clock.UtcNow;
    var refundCase = new OrderRefundCase
    {
      Id = Guid.NewGuid(),
      OrderId = lockedOrder.Id,
      CustomerId = customerId,
      ShopId = lockedOrder.ShopId,
      CaseNumber = $"RFD-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
      RequestedAmount = requestedAmount,
      ApprovedAmount = 0m,
      Reason = request.Reason.Trim(),
      Status = "Pending",
      SandboxSimulated = true,
      CustomerBankSnapshotJson = SerializeCustomerBankSnapshot(request.RefundBankName, request.RefundAccountNumber, request.RefundAccountHolderName),
      CreatedBy = customerId,
      CreatedAt = now,
      UpdatedAt = now
    };
    context.OrderRefundCases.Add(refundCase);
    context.OrderFinancialLedgerEntries.Add(new OrderFinancialLedgerEntry
    {
      Id = Guid.NewGuid(),
      OrderId = lockedOrder.Id,
      ShopId = lockedOrder.ShopId,
      CustomerId = customerId,
      RefundCaseId = refundCase.Id,
      EntryType = "ReservedForRefund",
      BalanceBucket = "Held",
      Amount = 0m,
      Description = $"Refund case {refundCase.CaseNumber} opened for {requestedAmount:N0} VND (sandbox simulation).",
      IdempotencyKey = $"refund-open:{refundCase.Id}",
      CreatedBy = customerId,
      CreatedAt = now,
      UpdatedAt = now
    });

    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return MapRefundCase(refundCase, lockedOrder.OrderNumber);
  }

  public async Task<OrderRefundCaseResponse> ProcessRefundCaseAsync(
      Guid refundCaseId, Guid adminUserId, ProcessOrderRefundCaseRequest request, CancellationToken cancellationToken = default)
  {
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    var refundCase = await context.OrderRefundCases.FromSqlInterpolated(
            $"SELECT * FROM order_refund_cases WHERE id = {refundCaseId} FOR UPDATE")
        .SingleOrDefaultAsync(cancellationToken)
        ?? throw new HttpException(404, "Refund case was not found.");
    if (refundCase.Status is not ("Pending" or "Processing"))
      throw new HttpException(409, "Refund case has already been processed.", OrderErrorCodes.StateConflict);

    var lockedOrder = await context.Orders.FromSqlInterpolated(
            $"SELECT * FROM orders WHERE id = {refundCase.OrderId} FOR UPDATE")
        .SingleAsync(cancellationToken);

    var now = clock.UtcNow;
    refundCase.ProcessedByAdminId = adminUserId;
    refundCase.ProcessedAt = now;
    refundCase.AdminNotes = request.Notes.Trim();
    refundCase.UpdatedBy = adminUserId;
    refundCase.UpdatedAt = now;

    if (request.Approve)
    {
      var approvedAmount = request.ApprovedAmount.HasValue && request.ApprovedAmount.Value > 0m
          ? Math.Min(refundCase.RequestedAmount, decimal.Round(request.ApprovedAmount.Value, 2, MidpointRounding.AwayFromZero))
          : refundCase.RequestedAmount;

      refundCase.ApprovedAmount = approvedAmount;
      refundCase.Status = "CompletedSimulated";
      refundCase.SandboxSimulated = string.IsNullOrWhiteSpace(request.TransferProofImageUrl);
      refundCase.TransferProofImageUrl = string.IsNullOrWhiteSpace(request.TransferProofImageUrl)
          ? null
          : request.TransferProofImageUrl.Trim();
      refundCase.RefundReferenceCode = string.IsNullOrWhiteSpace(request.RefundReferenceCode)
          ? $"SIM-RFD-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant()
          : request.RefundReferenceCode.Trim();

      await ApplyCompletedRefundCoreAsync(
          lockedOrder,
          refundCase,
          approvedAmount,
          adminUserId,
          now,
          $"refund-complete:{refundCase.Id}",
          $"Refund {refundCase.CaseNumber} completed ({refundCase.RefundReferenceCode}).",
          cancellationToken);
    }
    else
    {
      refundCase.Status = "Rejected";
      refundCase.ApprovedAmount = 0m;
    }

    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return MapRefundCase(refundCase, lockedOrder.OrderNumber);
  }

  public async Task<SupplierPayoutAccountResponse?> GetSupplierPayoutAccountAsync(
      Guid supplierUserId, CancellationToken cancellationToken = default)
  {
    var account = await context.SupplierPayoutAccounts.AsNoTracking()
        .FirstOrDefaultAsync(item => item.SupplierUserId == supplierUserId && !item.IsDeleted, cancellationToken);
    return account is null ? null : MapPayoutAccount(account);
  }

  public async Task<SupplierPayoutAccountResponse> UpsertSupplierPayoutAccountAsync(
      Guid supplierUserId, UpsertSupplierPayoutAccountRequest request, CancellationToken cancellationToken = default)
  {
    var now = clock.UtcNow;
    var account = await context.SupplierPayoutAccounts
        .FirstOrDefaultAsync(item => item.SupplierUserId == supplierUserId && !item.IsDeleted, cancellationToken);

    if (account is null)
    {
      account = new SupplierPayoutAccount
      {
        Id = Guid.NewGuid(),
        SupplierUserId = supplierUserId,
        BankCode = request.BankCode.Trim().ToUpperInvariant(),
        BankName = request.BankName.Trim(),
        AccountNumber = request.AccountNumber.Trim(),
        AccountHolderName = request.AccountHolderName.Trim().ToUpperInvariant(),
        BranchName = string.IsNullOrWhiteSpace(request.BranchName) ? null : request.BranchName.Trim(),
        IsVerifiedSimulated = true,
        Version = 1L,
        CreatedBy = supplierUserId,
        CreatedAt = now,
        UpdatedAt = now
      };
      context.SupplierPayoutAccounts.Add(account);
    }
    else
    {
      account.BankCode = request.BankCode.Trim().ToUpperInvariant();
      account.BankName = request.BankName.Trim();
      account.AccountNumber = request.AccountNumber.Trim();
      account.AccountHolderName = request.AccountHolderName.Trim().ToUpperInvariant();
      account.BranchName = string.IsNullOrWhiteSpace(request.BranchName) ? null : request.BranchName.Trim();
      account.IsVerifiedSimulated = true;
      account.Version += 1;
      account.UpdatedBy = supplierUserId;
      account.UpdatedAt = now;
    }

    await context.SaveChangesAsync(cancellationToken);
    return MapPayoutAccount(account);
  }

  public async Task<SupplierFinancialOverviewResponse> GetSupplierFinancialOverviewAsync(
      Guid supplierUserId, CancellationToken cancellationToken = default)
  {
    var shopIds = await GetOwnedShopIdsAsync(supplierUserId, cancellationToken);
    var account = await GetSupplierPayoutAccountAsync(supplierUserId, cancellationToken);
    if (shopIds.Count == 0)
    {
      return new SupplierFinancialOverviewResponse(true, 0m, 0m, 0m, 0m, 0m, 0m, 0m, account, []);
    }

    var ledger = await context.OrderFinancialLedgerEntries.AsNoTracking()
        .Where(item => shopIds.Contains(item.ShopId) && !item.IsDeleted)
        .ToListAsync(cancellationToken);
    var payoutRequests = await context.SupplierPayoutRequests.AsNoTracking()
        .Where(item => shopIds.Contains(item.ShopId) && !item.IsDeleted)
        .OrderByDescending(item => item.CreatedAt)
        .Take(50)
        .ToListAsync(cancellationToken);
    var orderIds = payoutRequests.Select(x => x.OrderId).Distinct().ToArray();
    var orderNumbers = await context.Orders.AsNoTracking()
        .Where(o => orderIds.Contains(o.Id))
        .ToDictionaryAsync(o => o.Id, o => o.OrderNumber, cancellationToken);

    var totals = ComputeBalancesFromLedger(ledger, payoutRequests);
    return new SupplierFinancialOverviewResponse(
        true,
        totals.Captured,
        totals.Held,
        totals.Disputed,
        totals.Refunded,
        totals.Eligible,
        totals.PayoutRequested,
        totals.PaidOutSimulated,
        account,
        payoutRequests.Select(item => MapPayoutRequest(item, orderNumbers.GetValueOrDefault(item.OrderId))).ToList());
  }

  public async Task<SupplierPayoutRequestResponse> CreatePayoutRequestAsync(
      Order order, Guid supplierUserId, CreateOrderPayoutRequest request, CancellationToken cancellationToken = default)
  {
    context.ChangeTracker.Clear();
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    var lockedOrder = await context.Orders.FromSqlInterpolated(
            $"SELECT * FROM orders WHERE id = {order.Id} FOR UPDATE")
        .SingleOrDefaultAsync(cancellationToken)
        ?? throw new HttpException(404, B2BOrderingMessages.OrderNotFound);

    if (lockedOrder.Status != OrderStatus.Delivered)
      throw new HttpException(409, "Payout can only be requested after the customer confirms delivery.", OrderErrorCodes.StateConflict);

    var hasOpenDispute = await context.OrderDeliveryDisputes
        .AnyAsync(item => item.OrderId == lockedOrder.Id
            && (item.Status == "Open" || item.Status == "AwaitingSupplierRebuttal")
            && !item.IsDeleted, cancellationToken);
    if (hasOpenDispute)
      throw new HttpException(409, "Payout is locked while a delivery dispute is open.", OrderErrorCodes.StateConflict);

    var hasInlineBank = !string.IsNullOrWhiteSpace(request.BankName)
        || !string.IsNullOrWhiteSpace(request.AccountNumber)
        || !string.IsNullOrWhiteSpace(request.AccountHolderName);
    if (hasInlineBank && (string.IsNullOrWhiteSpace(request.BankName)
        || string.IsNullOrWhiteSpace(request.AccountNumber)
        || string.IsNullOrWhiteSpace(request.AccountHolderName)))
    {
      throw new HttpException(400, "Vui lòng cung cấp đầy đủ Tên ngân hàng, Số tài khoản và Tên chủ tài khoản.", OrderErrorCodes.ValidationError);
    }

    var now = clock.UtcNow;
    var payoutAccount = await context.SupplierPayoutAccounts
        .FirstOrDefaultAsync(item => item.SupplierUserId == supplierUserId && !item.IsDeleted, cancellationToken);
    if (payoutAccount is null)
    {
      if (!hasInlineBank)
        throw new HttpException(422, "Configure or provide a supplier payout bank account before requesting disbursement.", OrderErrorCodes.ValidationError);

      payoutAccount = new SupplierPayoutAccount
      {
        Id = Guid.NewGuid(),
        SupplierUserId = supplierUserId,
        BankCode = string.IsNullOrWhiteSpace(request.BankCode)
            ? request.BankName!.Trim().ToUpperInvariant()
            : request.BankCode.Trim().ToUpperInvariant(),
        BankName = request.BankName!.Trim(),
        AccountNumber = request.AccountNumber!.Trim(),
        AccountHolderName = request.AccountHolderName!.Trim().ToUpperInvariant(),
        BranchName = string.IsNullOrWhiteSpace(request.BranchName) ? null : request.BranchName.Trim(),
        IsVerifiedSimulated = true,
        Version = 1L,
        CreatedBy = supplierUserId,
        CreatedAt = now,
        UpdatedAt = now
      };
      context.SupplierPayoutAccounts.Add(payoutAccount);
    }

    var ledger = await context.OrderFinancialLedgerEntries
        .Where(item => item.OrderId == lockedOrder.Id && !item.IsDeleted)
        .ToListAsync(cancellationToken);
    var payouts = await context.SupplierPayoutRequests
        .Where(item => item.OrderId == lockedOrder.Id && !item.IsDeleted)
        .ToListAsync(cancellationToken);

    var balances = ComputeBalancesFromLedger(ledger, payouts);
    if (balances.Eligible <= 0m)
      throw new HttpException(409, "No eligible balance is available for payout on this order.", OrderErrorCodes.StateConflict);

    var snapshotBankName = hasInlineBank ? request.BankName!.Trim() : payoutAccount.BankName;
    var snapshotBankCode = hasInlineBank
        ? (string.IsNullOrWhiteSpace(request.BankCode) ? snapshotBankName.ToUpperInvariant() : request.BankCode.Trim().ToUpperInvariant())
        : payoutAccount.BankCode;
    var snapshotAccountNumber = hasInlineBank ? request.AccountNumber!.Trim() : payoutAccount.AccountNumber;
    var snapshotHolder = hasInlineBank ? request.AccountHolderName!.Trim().ToUpperInvariant() : payoutAccount.AccountHolderName;
    var snapshotBranch = hasInlineBank
        ? (string.IsNullOrWhiteSpace(request.BranchName) ? null : request.BranchName.Trim())
        : payoutAccount.BranchName;

    var payoutReq = new SupplierPayoutRequest
    {
      Id = Guid.NewGuid(),
      OrderId = lockedOrder.Id,
      ShopId = lockedOrder.ShopId,
      SupplierUserId = supplierUserId,
      PayoutAccountId = payoutAccount.Id,
      RequestNumber = $"PYT-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
      Amount = balances.Eligible,
      Status = "Pending",
      SandboxSimulated = true,
      BankSnapshotJson = JsonSerializer.Serialize(new
      {
        BankCode = snapshotBankCode,
        BankName = snapshotBankName,
        AccountNumber = snapshotAccountNumber,
        AccountHolderName = snapshotHolder,
        BranchName = snapshotBranch
      }),
      AdminNotes = request.Notes?.Trim(),
      CreatedBy = supplierUserId,
      CreatedAt = now,
      UpdatedAt = now
    };
    context.SupplierPayoutRequests.Add(payoutReq);
    context.OrderFinancialLedgerEntries.Add(new OrderFinancialLedgerEntry
    {
      Id = Guid.NewGuid(),
      OrderId = lockedOrder.Id,
      ShopId = lockedOrder.ShopId,
      CustomerId = lockedOrder.CustomerId,
      PayoutRequestId = payoutReq.Id,
      EntryType = "PayoutRequested",
      BalanceBucket = "Eligible",
      Amount = 0m,
      Description = $"Supplier requested simulated payout {payoutReq.RequestNumber} ({payoutReq.Amount:N0} VND).",
      IdempotencyKey = $"payout-req:{payoutReq.Id}",
      CreatedBy = supplierUserId,
      CreatedAt = now,
      UpdatedAt = now
    });

    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return MapPayoutRequest(payoutReq, lockedOrder.OrderNumber);
  }

  public async Task<SupplierPayoutRequestResponse> ProcessPayoutRequestAsync(
      Guid payoutRequestId, Guid adminUserId, ProcessOrderPayoutRequest request, CancellationToken cancellationToken = default)
  {
    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    var payoutReq = await context.SupplierPayoutRequests.FromSqlInterpolated(
            $"SELECT * FROM supplier_payout_requests WHERE id = {payoutRequestId} FOR UPDATE")
        .SingleOrDefaultAsync(cancellationToken)
        ?? throw new HttpException(404, "Payout request was not found.");
    if (payoutReq.Status != "Pending")
      throw new HttpException(409, "Payout request has already been processed.", OrderErrorCodes.StateConflict);

    var lockedOrder = await context.Orders.FromSqlInterpolated(
            $"SELECT * FROM orders WHERE id = {payoutReq.OrderId} FOR UPDATE")
        .SingleAsync(cancellationToken);

    var now = clock.UtcNow;
    payoutReq.ProcessedByAdminId = adminUserId;
    payoutReq.ProcessedAt = now;
    payoutReq.AdminNotes = request.Notes.Trim();
    payoutReq.UpdatedBy = adminUserId;
    payoutReq.UpdatedAt = now;

    if (request.Approve)
    {
      payoutReq.Status = "PaidOutSimulated";
      payoutReq.SandboxSimulated = string.IsNullOrWhiteSpace(request.TransferProofImageUrl);
      payoutReq.TransferProofImageUrl = string.IsNullOrWhiteSpace(request.TransferProofImageUrl)
          ? null
          : request.TransferProofImageUrl.Trim();
      payoutReq.PayoutReferenceCode = string.IsNullOrWhiteSpace(request.PayoutReferenceCode)
          ? $"{(string.IsNullOrWhiteSpace(request.TransferProofImageUrl) ? "SIM-PYT" : "TRF-PYT")}-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant()
          : request.PayoutReferenceCode.Trim();

      context.OrderFinancialLedgerEntries.Add(new OrderFinancialLedgerEntry
      {
        Id = Guid.NewGuid(),
        OrderId = lockedOrder.Id,
        ShopId = lockedOrder.ShopId,
        CustomerId = lockedOrder.CustomerId,
        PayoutRequestId = payoutReq.Id,
        EntryType = "PaidOutSimulated",
        BalanceBucket = "PaidOutSimulated",
        Amount = payoutReq.Amount,
        Description = $"Disbursement {payoutReq.RequestNumber} completed ({payoutReq.PayoutReferenceCode}).",
        IdempotencyKey = $"payout-complete:{payoutReq.Id}",
        CreatedBy = adminUserId,
        CreatedAt = now,
        UpdatedAt = now
      });

      context.Notifications.Add(new Notification
      {
        UserId = payoutReq.SupplierUserId,
        Type = "order.payout_simulated",
        Title = "Payout disbursement completed",
        Content = $"Payout {payoutReq.RequestNumber} ({payoutReq.Amount:N0} VND) for order {lockedOrder.OrderNumber} was completed ({payoutReq.PayoutReferenceCode}).",
        ReferenceType = OrderReferenceType,
        ReferenceId = lockedOrder.Id
      });
    }
    else
    {
      payoutReq.Status = "Rejected";
    }

    await context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return MapPayoutRequest(payoutReq, lockedOrder.OrderNumber);
  }

  public async Task<AdminOrderOperationsOverviewResponse> GetAdminOperationsOverviewAsync(CancellationToken cancellationToken = default)
  {
    var ledger = await context.OrderFinancialLedgerEntries.AsNoTracking()
        .Where(item => !item.IsDeleted)
        .GroupBy(item => new { item.EntryType, item.BalanceBucket })
        .Select(group => new OrderFinancialLedgerEntry
        {
          EntryType = group.Key.EntryType,
          BalanceBucket = group.Key.BalanceBucket,
          Amount = group.Sum(item => item.Amount)
        })
        .ToListAsync(cancellationToken);
    var payouts = await context.SupplierPayoutRequests.AsNoTracking()
        .Where(item => !item.IsDeleted)
        .OrderByDescending(item => item.CreatedAt)
        .Take(100)
        .ToListAsync(cancellationToken);
    var refunds = await context.OrderRefundCases.AsNoTracking()
        .Where(item => !item.IsDeleted)
        .OrderByDescending(item => item.CreatedAt)
        .Take(100)
        .ToListAsync(cancellationToken);
    var disputes = await context.OrderDeliveryDisputes.AsNoTracking()
        .Where(item => !item.IsDeleted)
        .OrderByDescending(item => item.CreatedAt)
        .Take(100)
        .ToListAsync(cancellationToken);

    var orderIds = payouts.Select(x => x.OrderId)
        .Concat(refunds.Select(x => x.OrderId))
        .Concat(disputes.Select(x => x.OrderId))
        .Distinct()
        .ToArray();
    var orderNumbers = await context.Orders.AsNoTracking()
        .Where(o => orderIds.Contains(o.Id))
        .ToDictionaryAsync(o => o.Id, o => o.OrderNumber, cancellationToken);
    var paidAmounts = await GetOrderPaidBalancesAsync(disputes.Select(x => x.OrderId), cancellationToken);

    var pendingPayoutAmount = await context.SupplierPayoutRequests.AsNoTracking()
        .Where(item => !item.IsDeleted && item.Status == "Pending")
        .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0m;
    var totals = ComputeBalancesFromLedger(ledger,
        [new SupplierPayoutRequest { Status = "Pending", Amount = pendingPayoutAmount }]);
    return new AdminOrderOperationsOverviewResponse(
        true,
        totals.Held,
        totals.Disputed,
        totals.Eligible,
        totals.Refunded,
        totals.PaidOutSimulated,
        refunds.Select(x => MapRefundCase(x, orderNumbers.GetValueOrDefault(x.OrderId))).ToList(),
        payouts.Select(x => MapPayoutRequest(x, orderNumbers.GetValueOrDefault(x.OrderId))).ToList(),
        disputes.Select(x => MapDispute(x, orderNumbers.GetValueOrDefault(x.OrderId), paidAmounts.GetValueOrDefault(x.OrderId))).ToList());
  }

  public async Task<PagedResult<OrderRefundCaseResponse>> GetAdminRefundCasesPageAsync(
      AdminOperationPageQueryRequest query, CancellationToken cancellationToken = default)
  {
    IQueryable<OrderRefundCase> rows = context.OrderRefundCases.AsNoTracking().Where(item => !item.IsDeleted);
    if (!string.IsNullOrWhiteSpace(query.Status))
    {
      var statusFilter = query.Status.Trim();
      if (string.Equals(statusFilter, "Completed", StringComparison.OrdinalIgnoreCase)
          || string.Equals(statusFilter, "Approved", StringComparison.OrdinalIgnoreCase))
      {
        rows = rows.Where(item => item.Status == "Completed" || item.Status == "CompletedSimulated");
      }
      else
      {
        rows = rows.Where(item => item.Status == statusFilter);
      }
    }
    if (query.ShopId.HasValue) rows = rows.Where(item => item.ShopId == query.ShopId.Value);
    if (query.MinAmount.HasValue) rows = rows.Where(item => item.RequestedAmount >= query.MinAmount.Value);
    if (query.MaxAmount.HasValue) rows = rows.Where(item => item.RequestedAmount <= query.MaxAmount.Value);
    if (query.From.HasValue) rows = rows.Where(item => item.CreatedAt >= query.From.Value);
    if (query.To.HasValue) rows = rows.Where(item => item.CreatedAt <= query.To.Value);
    if (!string.IsNullOrWhiteSpace(query.Q))
    {
      var term = $"%{query.Q.Trim()}%";
      rows = rows.Where(item => EF.Functions.ILike(item.CaseNumber, term)
          || context.Orders.Any(order => order.Id == item.OrderId && EF.Functions.ILike(order.OrderNumber, term)));
    }
    var total = await rows.CountAsync(cancellationToken);
    rows = query.Sort switch
    {
      "createdAtAsc" => rows.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
      "amountAsc" => rows.OrderBy(item => item.RequestedAmount).ThenBy(item => item.Id),
      "amountDesc" => rows.OrderByDescending(item => item.RequestedAmount).ThenByDescending(item => item.Id),
      _ => rows.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
    };
    var items = await PagedQueryHelper.ApplyPaging(rows, query).ToListAsync(cancellationToken);
    var numbers = await GetOrderNumbersAsync(items.Select(item => item.OrderId), cancellationToken);
    return new PagedResult<OrderRefundCaseResponse>(items.Select(item =>
        MapRefundCase(item, numbers.GetValueOrDefault(item.OrderId))).ToList(), query.Page, query.PageSize, total);
  }

  public async Task<PagedResult<SupplierPayoutRequestResponse>> GetAdminPayoutRequestsPageAsync(
      AdminOperationPageQueryRequest query, CancellationToken cancellationToken = default)
  {
    IQueryable<SupplierPayoutRequest> rows = context.SupplierPayoutRequests.AsNoTracking().Where(item => !item.IsDeleted);
    if (!string.IsNullOrWhiteSpace(query.Status)) rows = rows.Where(item => item.Status == query.Status);
    if (query.ShopId.HasValue) rows = rows.Where(item => item.ShopId == query.ShopId.Value);
    if (query.MinAmount.HasValue) rows = rows.Where(item => item.Amount >= query.MinAmount.Value);
    if (query.MaxAmount.HasValue) rows = rows.Where(item => item.Amount <= query.MaxAmount.Value);
    if (query.From.HasValue) rows = rows.Where(item => item.CreatedAt >= query.From.Value);
    if (query.To.HasValue) rows = rows.Where(item => item.CreatedAt <= query.To.Value);
    if (!string.IsNullOrWhiteSpace(query.Q))
    {
      var term = $"%{query.Q.Trim()}%";
      rows = rows.Where(item => EF.Functions.ILike(item.RequestNumber, term)
          || context.Orders.Any(order => order.Id == item.OrderId && EF.Functions.ILike(order.OrderNumber, term)));
    }
    var total = await rows.CountAsync(cancellationToken);
    rows = query.Sort switch
    {
      "createdAtAsc" => rows.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
      "amountAsc" => rows.OrderBy(item => item.Amount).ThenBy(item => item.Id),
      "amountDesc" => rows.OrderByDescending(item => item.Amount).ThenByDescending(item => item.Id),
      _ => rows.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
    };
    var items = await PagedQueryHelper.ApplyPaging(rows, query).ToListAsync(cancellationToken);
    var numbers = await GetOrderNumbersAsync(items.Select(item => item.OrderId), cancellationToken);
    return new PagedResult<SupplierPayoutRequestResponse>(items.Select(item =>
        MapPayoutRequest(item, numbers.GetValueOrDefault(item.OrderId))).ToList(), query.Page, query.PageSize, total);
  }

  public async Task<PagedResult<OrderDeliveryDisputeResponse>> GetAdminDeliveryDisputesPageAsync(
      AdminOperationPageQueryRequest query, CancellationToken cancellationToken = default)
  {
    IQueryable<OrderDeliveryDispute> rows = context.OrderDeliveryDisputes.AsNoTracking().Where(item => !item.IsDeleted);
    if (!string.IsNullOrWhiteSpace(query.Status))
    {
      var statusFilter = query.Status.Trim();
      if (string.Equals(statusFilter, "Resolved", StringComparison.OrdinalIgnoreCase))
        rows = rows.Where(item => item.Status.StartsWith("Resolved"));
      else
        rows = rows.Where(item => item.Status == statusFilter);
    }
    if (query.ShopId.HasValue) rows = rows.Where(item => item.ShopId == query.ShopId.Value);
    if (query.From.HasValue) rows = rows.Where(item => item.CreatedAt >= query.From.Value);
    if (query.To.HasValue) rows = rows.Where(item => item.CreatedAt <= query.To.Value);
    if (!string.IsNullOrWhiteSpace(query.Q))
    {
      var term = $"%{query.Q.Trim()}%";
      rows = rows.Where(item => context.Orders.Any(order => order.Id == item.OrderId
          && EF.Functions.ILike(order.OrderNumber, term)));
    }
    var total = await rows.CountAsync(cancellationToken);
    rows = query.Sort == "createdAtAsc"
        ? rows.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id)
        : rows.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id);
    var items = await PagedQueryHelper.ApplyPaging(rows, query).ToListAsync(cancellationToken);
    var disputeOrderIds = items.Select(item => item.OrderId).ToArray();
    var numbers = await GetOrderNumbersAsync(disputeOrderIds, cancellationToken);
    var paidAmounts = await GetOrderPaidBalancesAsync(disputeOrderIds, cancellationToken);
    return new PagedResult<OrderDeliveryDisputeResponse>(items.Select(item =>
        MapDispute(item, numbers.GetValueOrDefault(item.OrderId), paidAmounts.GetValueOrDefault(item.OrderId))).ToList(), query.Page, query.PageSize, total);
  }

  private Task<Dictionary<Guid, string>> GetOrderNumbersAsync(IEnumerable<Guid> ids,
      CancellationToken cancellationToken)
  {
    var orderIds = ids.Distinct().ToArray();
    return context.Orders.AsNoTracking().Where(order => orderIds.Contains(order.Id))
        .ToDictionaryAsync(order => order.Id, order => order.OrderNumber, cancellationToken);
  }

  public async Task<int> ExpireOverdueOrdersAsync(CancellationToken cancellationToken = default)
  {
    var now = clock.UtcNow;
    var candidateIds = await context.Orders.AsNoTracking()
        .Where(order => !order.IsDeleted
            && ((order.Status == OrderStatus.Pending && order.ResponseDeadlineAt != null && order.ResponseDeadlineAt <= now)
                || (order.Status == OrderStatus.AwaitingCustomerDeliveryApproval && order.DeliveryApprovalDeadlineAt != null && order.DeliveryApprovalDeadlineAt <= now)
                || (order.Status == OrderStatus.Confirmed && order.ReservationExpiresAt != null && order.ReservationExpiresAt <= now)))
        .OrderBy(order => order.ReservationExpiresAt)
        .Take(50)
        .Select(order => order.Id)
        .ToListAsync(cancellationToken);

    var expiredCount = 0;
    foreach (var candidateId in candidateIds)
    {
      await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
      var lockedOrder = await context.Orders.FromSqlInterpolated(
              $"SELECT * FROM orders WHERE id = {candidateId} FOR UPDATE SKIP LOCKED")
          .SingleOrDefaultAsync(cancellationToken);
      if (lockedOrder is null || lockedOrder.IsDeleted)
        continue;

      var stillOverdue =
          (lockedOrder.Status == OrderStatus.Pending && lockedOrder.ResponseDeadlineAt <= now)
          || (lockedOrder.Status == OrderStatus.AwaitingCustomerDeliveryApproval && lockedOrder.DeliveryApprovalDeadlineAt <= now)
          || (lockedOrder.Status == OrderStatus.Confirmed && lockedOrder.ReservationExpiresAt <= now);
      if (!stillOverdue)
        continue;

      var paidAmount = await context.Payments
          .Where(item => item.OrderId == lockedOrder.Id && item.Status == PaymentStatus.Completed && !item.IsDeleted)
          .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0m;

      var fromStatus = lockedOrder.Status;
      var reason = fromStatus switch
      {
        OrderStatus.Pending => "Order expired because supplier response SLA (12h) elapsed.",
        OrderStatus.AwaitingCustomerDeliveryApproval => "Order expired because customer delivery proposal approval SLA (12h) elapsed.",
        _ => paidAmount > 0m
            ? "Order cancelled because custom packaging mockup reservation window (5 business days) elapsed; refund case opened."
            : "Order expired because initial deposit/payment deadline (24h) elapsed."
      };

      if (paidAmount > 0m)
      {
        await ExecuteOrderCancellationCoreAsync(lockedOrder, null, lockedOrder.CustomerId, reason, paidAmount, now, cancellationToken);
      }
      else
      {
        lockedOrder.Status = OrderStatus.Expired;
        lockedOrder.CancelledAt = now;
        lockedOrder.CancellationReason = reason;
        lockedOrder.ReservationExpiresAt = null;
        lockedOrder.Version += 1;
        lockedOrder.UpdatedAt = now;

        await CancelOpenPaymentScheduleCoreAsync(lockedOrder.Id, now, cancellationToken);
        await ReleaseOrderStockIdempotentCoreAsync(lockedOrder, lockedOrder.CustomerId, reason, now, cancellationToken);

        context.WorkflowEvents.Add(new WorkflowEvent
        {
          AggregateType = OrderAggregateType,
          AggregateId = lockedOrder.Id,
          EventType = "OrderExpired",
          FromStatus = fromStatus.ToString(),
          ToStatus = OrderStatus.Expired.ToString(),
          Reason = reason
        });
        context.Notifications.Add(new Notification
        {
          UserId = lockedOrder.CustomerId,
          Type = "order.expired",
          Title = "Order expired",
          Content = $"Order {lockedOrder.OrderNumber} expired: {reason}",
          ReferenceType = OrderReferenceType,
          ReferenceId = lockedOrder.Id
        });
      }

      await context.SaveChangesAsync(cancellationToken);
      await transaction.CommitAsync(cancellationToken);
      expiredCount++;
    }

    return expiredCount;
  }

  public async Task<int> AutoConfirmDeliveredOrdersAsync(CancellationToken cancellationToken = default)
  {
    var now = clock.UtcNow;
    var cutoff = now.AddHours(-OrderPolicyDefaults.DeliveryAutoConfirmHours);

    var candidateIds = await (
        from order in context.Orders.AsNoTracking()
        where !order.IsDeleted && order.Status == OrderStatus.Shipped
        join delivery in context.Deliveries.AsNoTracking() on order.Id equals delivery.OrderId
        where !delivery.IsDeleted
        join proof in context.DeliveryProofs.AsNoTracking() on delivery.Id equals proof.DeliveryId
        where !proof.IsDeleted && proof.ProofType == ProofType.Delivery && proof.CreatedAt <= cutoff
        where !context.OrderDeliveryDisputes.Any(dispute =>
            dispute.OrderId == order.Id &&
            !dispute.IsDeleted &&
            (dispute.Status == null || (!dispute.Status.StartsWith("Resolved") && dispute.Status != "Closed")))
        select order.Id
    ).Distinct().Take(50).ToListAsync(cancellationToken);

    var confirmedCount = 0;
    foreach (var candidateId in candidateIds)
    {
      context.ChangeTracker.Clear();
      await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
      var lockedOrder = context.Database.IsRelational()
          ? await context.Orders.FromSqlInterpolated(
                  $"SELECT * FROM orders WHERE id = {candidateId} FOR UPDATE SKIP LOCKED")
              .SingleOrDefaultAsync(cancellationToken)
          : await context.Orders.SingleOrDefaultAsync(o => o.Id == candidateId, cancellationToken);
      if (lockedOrder is null || lockedOrder.IsDeleted || lockedOrder.Status != OrderStatus.Shipped)
        continue;

      var hasUnresolvedDispute = await context.OrderDeliveryDisputes.AsNoTracking()
          .AnyAsync(item => item.OrderId == lockedOrder.Id &&
              !item.IsDeleted &&
              (item.Status == null || (!item.Status.StartsWith("Resolved") && item.Status != "Closed")), cancellationToken);
      if (hasUnresolvedDispute)
        continue;

      var delivery = await context.Deliveries
          .Where(item => item.OrderId == lockedOrder.Id && !item.IsDeleted)
          .OrderByDescending(item => item.CreatedAt)
          .FirstOrDefaultAsync(cancellationToken);
      if (delivery is null)
        continue;

      var hasDeliveryProof = await context.DeliveryProofs.AsNoTracking()
          .AnyAsync(item => item.DeliveryId == delivery.Id && item.ProofType == ProofType.Delivery && !item.IsDeleted && item.CreatedAt <= cutoff, cancellationToken);
      if (!hasDeliveryProof)
        continue;

      await ExecuteOrderDeliveryConfirmationCoreAsync(
          lockedOrder, delivery, lockedOrder.CustomerId, "Tự xác nhận sau 72h", isAutoConfirmed: true, cancellationToken);

      await context.SaveChangesAsync(cancellationToken);
      await transaction.CommitAsync(cancellationToken);
      confirmedCount++;
    }

    return confirmedCount;
  }

  private void BuildInitialObligationsAndParts(Order order, Guid actorUserId, DateTime now, DateTime depositDeadline)
  {
    var depositApplies = order.DepositRequiredSnapshot && order.DepositPercentSnapshot is >= 10m and <= 50m;
    if (depositApplies)
    {
      var depositAmount = decimal.Round(order.TotalAmount * order.DepositPercentSnapshot / 100m, 0, MidpointRounding.AwayFromZero);
      var balanceAmount = order.TotalAmount - depositAmount;
      if (depositAmount > 0m && balanceAmount > 0m)
      {
        AddObligationWithParts(order, 1, "Deposit", $"Order deposit ({order.DepositPercentSnapshot:0.##}%)",
            depositAmount, true, true, depositDeadline, null, actorUserId, now);
        AddObligationWithParts(order, 2, "RemainingBalance", "Remaining balance before shipping",
            balanceAmount, false, true, null, null, actorUserId, now);
        return;
      }
    }

    AddObligationWithParts(order, 1, "Full", "Full order payment",
        order.TotalAmount, true, true, depositDeadline, null, actorUserId, now);
  }

  private void AddObligationWithParts(
      Order order, int sequenceNo, string obligationType, string title, decimal totalAmount,
      bool requiredBeforeProduction, bool requiredBeforeShipping, DateTime? dueAt, Guid? sourceRefId,
      Guid actorUserId, DateTime now)
  {
    var obligation = new OrderPaymentObligation
    {
      Id = Guid.NewGuid(),
      OrderId = order.Id,
      SequenceNo = sequenceNo,
      ObligationType = obligationType,
      Title = title,
      TotalAmount = totalAmount,
      PaidAmount = 0m,
      Status = "Pending",
      IsRequiredBeforeProduction = requiredBeforeProduction,
      IsRequiredBeforeShipping = requiredBeforeShipping,
      DueAt = dueAt,
      SourceReferenceId = sourceRefId,
      CreatedBy = actorUserId,
      CreatedAt = now,
      UpdatedAt = now
    };
    context.OrderPaymentObligations.Add(obligation);

    var splitAmounts = PriceQuoteVersionHelper.SplitIntoParts(totalAmount, OrderPolicyDefaults.VnPayMaxAmountPerPartVnd);
    for (var i = 0; i < splitAmounts.Count; i++)
    {
      context.OrderPaymentParts.Add(new OrderPaymentPart
      {
        Id = Guid.NewGuid(),
        OrderId = order.Id,
        ObligationId = obligation.Id,
        PartIndex = i + 1,
        TotalParts = splitAmounts.Count,
        Amount = splitAmounts[i],
        Status = "Pending",
        CreatedBy = actorUserId,
        CreatedAt = now,
        UpdatedAt = now
      });
    }
  }

  private async Task ExecuteOrderCancellationCoreAsync(
      Order lockedOrder, Guid? cancelRequestId, Guid actorUserId, string reason, decimal paidAmount,
      DateTime now, CancellationToken cancellationToken, decimal? refundAmount = null)
  {
    var suggestedRefund = refundAmount ?? paidAmount;
    var previousStatus = lockedOrder.Status;
    lockedOrder.Status = OrderStatus.Cancelled;
    lockedOrder.CancelledAt = now;
    lockedOrder.CancelledByUserId = actorUserId;
    lockedOrder.CancellationReason = reason;
    lockedOrder.ReservationExpiresAt = null;
    lockedOrder.Version += 1;
    lockedOrder.UpdatedAt = now;
    lockedOrder.UpdatedBy = actorUserId;

    await CancelOpenPaymentScheduleCoreAsync(lockedOrder.Id, now, cancellationToken);
    await ReleaseOrderStockIdempotentCoreAsync(lockedOrder, actorUserId, reason, now, cancellationToken);

    if (paidAmount > 0m)
    {
      var hasRefundCase = await context.OrderRefundCases
          .AnyAsync(item => item.OrderId == lockedOrder.Id && item.Status != "Rejected" && !item.IsDeleted, cancellationToken);
      if (!hasRefundCase)
      {
        var refundCase = new OrderRefundCase
        {
          Id = Guid.NewGuid(),
          OrderId = lockedOrder.Id,
          CustomerId = lockedOrder.CustomerId,
          ShopId = lockedOrder.ShopId,
          CancellationRequestId = cancelRequestId,
          CaseNumber = $"RFD-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
          RequestedAmount = suggestedRefund,
          ApprovedAmount = 0m,
          Reason = $"Automatic refund case after order cancellation: {reason}",
          Status = "Pending",
          SandboxSimulated = true,
          CreatedBy = actorUserId,
          CreatedAt = now,
          UpdatedAt = now
        };
        context.OrderRefundCases.Add(refundCase);
        context.OrderFinancialLedgerEntries.Add(new OrderFinancialLedgerEntry
        {
          Id = Guid.NewGuid(),
          OrderId = lockedOrder.Id,
          ShopId = lockedOrder.ShopId,
          CustomerId = lockedOrder.CustomerId,
          RefundCaseId = refundCase.Id,
          EntryType = "ReservedForRefund",
          BalanceBucket = "Held",
          Amount = 0m,
          Description = $"Refund case {refundCase.CaseNumber} queued ({suggestedRefund:N0} VND) after order cancellation.",
          IdempotencyKey = $"cancel-refund:{lockedOrder.Id}",
          CreatedBy = actorUserId,
          CreatedAt = now,
          UpdatedAt = now
        });
      }
    }

    context.WorkflowEvents.Add(new WorkflowEvent
    {
      AggregateType = OrderAggregateType,
      AggregateId = lockedOrder.Id,
      EventType = "OrderCancelled",
      FromStatus = previousStatus.ToString(),
      ToStatus = OrderStatus.Cancelled.ToString(),
      ActorUserId = actorUserId,
      Reason = reason
    });

    context.Notifications.Add(new Notification
    {
      UserId = lockedOrder.CustomerId,
      Type = "order.cancelled",
      Title = "Order cancelled",
      Content = $"Order {lockedOrder.OrderNumber} was cancelled. Reason: {reason}",
      ReferenceType = OrderReferenceType,
      ReferenceId = lockedOrder.Id
    });
  }

  private async Task CancelOpenPaymentScheduleCoreAsync(Guid orderId, DateTime now, CancellationToken cancellationToken)
  {
    await context.OrderPaymentParts
        .Where(item => item.OrderId == orderId && item.Status != "Paid" && item.Status != "Refunded")
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.Status, "Cancelled")
            .SetProperty(item => item.UpdatedAt, now), cancellationToken);

    await context.OrderPaymentObligations
        .Where(item => item.OrderId == orderId && item.Status != "Paid" && item.Status != "Refunded")
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.Status, "Cancelled")
            .SetProperty(item => item.UpdatedAt, now), cancellationToken);

    await context.Payments
        .Where(item => item.OrderId == orderId && (item.Status == PaymentStatus.Pending || item.Status == PaymentStatus.Processing))
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.Status, PaymentStatus.Failed)
            .SetProperty(item => item.UpdatedAt, now), cancellationToken);
  }

  private async Task ReleaseOrderStockIdempotentCoreAsync(
      Order lockedOrder, Guid actorUserId, string reason, DateTime now, CancellationToken cancellationToken)
  {
    var orderIdString = lockedOrder.Id.ToString();
    var alreadyReleasedVariants = await context.ProductInventoryMovements.AsNoTracking()
        .Where(item => (item.SourceType == "OrderCancellation" || item.SourceType == "OrderRelease")
            && item.SourceId == orderIdString)
        .Select(item => item.VariantId)
        .ToListAsync(cancellationToken);
    var releasedSet = alreadyReleasedVariants.ToHashSet();

    var orderItems = await context.OrderItems.AsNoTracking()
        .Where(item => item.OrderId == lockedOrder.Id && !item.IsDeleted)
        .ToListAsync(cancellationToken);

    foreach (var group in orderItems.GroupBy(item => item.VariantId).OrderBy(group => group.Key))
    {
      if (releasedSet.Contains(group.Key))
        continue;

      var quantity = group.Sum(item => item.Quantity);
      await context.ProductVariants.Where(variant => variant.Id == group.Key)
          .ExecuteUpdateAsync(setters => setters
              .SetProperty(variant => variant.StockQty, variant => variant.StockQty + quantity)
              .SetProperty(variant => variant.InventoryVersion, variant => variant.InventoryVersion + 1),
              cancellationToken);

      var inventory = await context.ProductVariants.AsNoTracking()
          .Where(variant => variant.Id == group.Key)
          .Select(variant => new { variant.ProductId, variant.ShopId, variant.StockQty })
          .SingleAsync(cancellationToken);

      context.ProductInventoryMovements.Add(new ProductInventoryMovement
      {
        ProductId = inventory.ProductId,
        VariantId = group.Key,
        ShopId = inventory.ShopId,
        ActorUserId = actorUserId,
        SourceType = "OrderCancellation",
        SourceId = orderIdString,
        Delta = quantity,
        PreviousQty = inventory.StockQty - quantity,
        CurrentQty = inventory.StockQty,
        Reason = reason.Length > 500 ? reason[..500] : reason
      });
    }

    await context.InventoryReservations
        .Where(item => item.OrderId == lockedOrder.Id && item.Status != "Released")
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.Status, "Released")
            .SetProperty(item => item.ReleasedAt, now)
            .SetProperty(item => item.ReleaseReason, reason.Length > 255 ? reason.Substring(0, 255) : reason)
            .SetProperty(item => item.UpdatedAt, now), cancellationToken);
  }

  private async Task UnlockHeldBalanceToEligibleCoreAsync(
      Order lockedOrder, DateTime now, Guid actorUserId, CancellationToken cancellationToken)
  {
    var unlockable = await GetUnlockableNetBalanceAsync(lockedOrder.Id, cancellationToken);
    if (unlockable <= 0m)
      return;

    var idempotencyKey = $"delivery-eligible:{lockedOrder.Id}:{lockedOrder.Version}";
    AddLedgerEntryIfMissing(new OrderFinancialLedgerEntry
    {
      Id = Guid.NewGuid(),
      OrderId = lockedOrder.Id,
      ShopId = lockedOrder.ShopId,
      CustomerId = lockedOrder.CustomerId,
      EntryType = "EligibleUnlocked",
      BalanceBucket = "Eligible",
      Amount = unlockable,
      Description = $"Held balance ({unlockable:N0} VND) unlocked to Eligible after delivery confirmation.",
      IdempotencyKey = idempotencyKey,
      CreatedBy = actorUserId,
      CreatedAt = now,
      UpdatedAt = now
    });
  }

  private async Task<decimal> GetUnlockableNetBalanceAsync(Guid orderId, CancellationToken cancellationToken)
  {
    var entries = await context.OrderFinancialLedgerEntries.AsNoTracking()
        .Where(item => item.OrderId == orderId && !item.IsDeleted)
        .ToListAsync(cancellationToken);
    var payouts = await context.SupplierPayoutRequests.AsNoTracking()
        .Where(item => item.OrderId == orderId && !item.IsDeleted)
        .ToListAsync(cancellationToken);
    var pendingRefunds = await context.OrderRefundCases.AsNoTracking()
        .Where(item => item.OrderId == orderId && item.Status == "Pending" && !item.IsDeleted)
        .SumAsync(item => (decimal?)item.RequestedAmount, cancellationToken) ?? 0m;
    var balances = ComputeBalancesFromLedger(entries, payouts);
    return Math.Max(0m, balances.Held - pendingRefunds);
  }

  private async Task<decimal> GetOrderPaidBalanceAsync(Guid orderId, CancellationToken cancellationToken)
  {
    var payments = await context.Payments.AsNoTracking()
        .Where(item => item.OrderId == orderId
            && (item.Status == PaymentStatus.Completed || item.Status == PaymentStatus.Refunded)
            && !item.IsDeleted)
        .Select(item => new { item.Amount, item.RefundedAmount })
        .ToListAsync(cancellationToken);
    return Math.Max(0m, payments.Sum(item => item.Amount - item.RefundedAmount));
  }

  private async Task<Dictionary<Guid, decimal>> GetOrderPaidBalancesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
  {
    var orderIds = ids.Distinct().ToArray();
    if (orderIds.Length == 0)
      return [];

    var payments = await context.Payments.AsNoTracking()
        .Where(item => item.OrderId.HasValue
            && orderIds.Contains(item.OrderId.Value)
            && (item.Status == PaymentStatus.Completed || item.Status == PaymentStatus.Refunded)
            && !item.IsDeleted)
        .Select(item => new { OrderId = item.OrderId!.Value, item.Amount, item.RefundedAmount })
        .ToListAsync(cancellationToken);

    return payments
        .GroupBy(item => item.OrderId)
        .ToDictionary(group => group.Key, group => Math.Max(0m, group.Sum(x => x.Amount - x.RefundedAmount)));
  }

  private async Task ApplyCompletedRefundCoreAsync(
      Order lockedOrder,
      OrderRefundCase refundCase,
      decimal approvedAmount,
      Guid adminUserId,
      DateTime now,
      string idempotencyKey,
      string ledgerDescription,
      CancellationToken cancellationToken)
  {
    context.OrderFinancialLedgerEntries.Add(new OrderFinancialLedgerEntry
    {
      Id = Guid.NewGuid(),
      OrderId = lockedOrder.Id,
      ShopId = lockedOrder.ShopId,
      CustomerId = lockedOrder.CustomerId,
      RefundCaseId = refundCase.Id,
      EntryType = "RefundedSimulated",
      BalanceBucket = "Refunded",
      Amount = approvedAmount,
      Description = ledgerDescription,
      IdempotencyKey = idempotencyKey,
      CreatedBy = adminUserId,
      CreatedAt = now,
      UpdatedAt = now
    });

    var completedPayments = await context.Payments
        .Where(item => item.OrderId == lockedOrder.Id && item.Status == PaymentStatus.Completed && !item.IsDeleted)
        .OrderBy(item => item.CreatedAt)
        .ToListAsync(cancellationToken);
    var remainingToMark = approvedAmount;
    foreach (var pay in completedPayments)
    {
      if (remainingToMark <= 0m) break;
      var refundableOnPay = pay.Amount - pay.RefundedAmount;
      if (refundableOnPay <= 0m) continue;
      var delta = Math.Min(remainingToMark, refundableOnPay);
      pay.RefundedAmount += delta;
      if (pay.RefundedAmount >= pay.Amount)
        pay.Status = PaymentStatus.Refunded;
      pay.RefundReason = refundCase.AdminNotes;
      pay.UpdatedAt = now;
      pay.UpdatedBy = adminUserId;
      remainingToMark -= delta;
    }

    context.Notifications.Add(new Notification
    {
      UserId = lockedOrder.CustomerId,
      Type = "order.refund_completed",
      Title = "Refund completed",
      Content = string.IsNullOrWhiteSpace(refundCase.TransferProofImageUrl)
            ? $"Refund case {refundCase.CaseNumber} for order {lockedOrder.OrderNumber} ({approvedAmount:N0} VND) was approved."
            : $"Refund case {refundCase.CaseNumber} for order {lockedOrder.OrderNumber} ({approvedAmount:N0} VND) has been transferred with proof receipt uploaded.",
      ReferenceType = OrderReferenceType,
      ReferenceId = lockedOrder.Id
    });
  }

  private static string? SerializeCustomerBankSnapshot(string? bankName, string? accountNumber, string? accountHolderName)
  {
    var bank = bankName?.Trim() ?? string.Empty;
    var number = accountNumber?.Trim() ?? string.Empty;
    var holder = accountHolderName?.Trim() ?? string.Empty;
    if (bank.Length == 0 && number.Length == 0 && holder.Length == 0)
      return null;
    if (bank.Length == 0 || number.Length == 0 || holder.Length == 0)
      throw new HttpException(400, "Please provide complete bank name, account number, and account holder name.", OrderErrorCodes.ValidationError);

    return JsonSerializer.Serialize(new
    {
      BankName = bank,
      AccountNumber = number,
      AccountHolderName = holder
    });
  }

  private void AddLedgerEntryIfMissing(OrderFinancialLedgerEntry entry)
  {
    if (context.ChangeTracker.Entries<OrderFinancialLedgerEntry>()
        .Any(tracked => tracked.Entity.IdempotencyKey == entry.IdempotencyKey))
      return;
    context.OrderFinancialLedgerEntries.Add(entry);
  }

  private static (decimal Captured, decimal Held, decimal Disputed, decimal Refunded, decimal Eligible, decimal PayoutRequested, decimal PaidOutSimulated)
      ComputeBalancesFromLedger(IReadOnlyList<OrderFinancialLedgerEntry> entries, IReadOnlyList<SupplierPayoutRequest> payoutRequests)
  {
    var captured = entries.Where(x => x.EntryType == "Captured").Sum(x => x.Amount);
    var refunded = entries.Where(x => x.EntryType == "RefundedSimulated").Sum(x => x.Amount);
    var disputed = Math.Max(0m, entries.Where(x => x.BalanceBucket == "Disputed").Sum(x => x.Amount));
    var unlockedToEligible = entries.Where(x => x.EntryType == "EligibleUnlocked").Sum(x => x.Amount);
    var paidOut = entries.Where(x => x.EntryType == "PaidOutSimulated").Sum(x => x.Amount);
    var payoutRequestedPending = payoutRequests.Where(x => x.Status == "Pending").Sum(x => x.Amount);

    var netRemainingInSystem = Math.Max(0m, captured - refunded - paidOut);
    var eligibleGross = Math.Min(netRemainingInSystem, Math.Max(0m, unlockedToEligible - paidOut));
    var eligibleAvailable = Math.Max(0m, eligibleGross - disputed - payoutRequestedPending);
    var held = Math.Max(0m, netRemainingInSystem - eligibleGross - disputed);

    return (captured, held, disputed, refunded, eligibleAvailable, payoutRequestedPending, paidOut);
  }

  private static OrderPaymentSummaryResponse BuildPaymentSummary(
      Order order,
      CustomPackagingSummaryResponse? packaging,
      IReadOnlyList<OrderPaymentObligation> obligations,
      IReadOnlyDictionary<Guid, List<OrderPaymentPart>> partsByObligation,
      IReadOnlyList<Payment> payments)
  {
    var latestPaymentByPart = payments
        .Where(p => p.PartId.HasValue)
        .GroupBy(p => p.PartId!.Value)
        .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

    var obligationDtos = obligations.Select(ob =>
    {
      var obParts = partsByObligation.GetValueOrDefault(ob.Id, [])
              .OrderBy(p => p.PartIndex)
              .Select(p =>
              {
                latestPaymentByPart.TryGetValue(p.Id, out var latestPay);
                return new OrderPaymentPartResponse(
                        p.Id, p.ObligationId, p.PartIndex, p.TotalParts, p.Amount, p.Status, p.PaidAt,
                        latestPay?.PaymentNumber, latestPay?.TransactionId);
              }).ToList();

      var remaining = Math.Max(0m, ob.TotalAmount - ob.PaidAmount);
      return new OrderPaymentObligationResponse(
              ob.Id, ob.SequenceNo, ob.ObligationType, ob.Title, ob.TotalAmount, ob.PaidAmount, remaining,
              ob.Status, ob.IsRequiredBeforeProduction, ob.IsRequiredBeforeShipping, ob.DueAt, ob.PaidAt, obParts);
    }).ToList();

    var totalPaid = payments.Where(p => p.Status is PaymentStatus.Completed or PaymentStatus.Refunded).Sum(p => p.Amount);
    var totalRefunded = payments.Sum(p => p.RefundedAmount);
    var netPaid = Math.Max(0m, totalPaid - totalRefunded);
    var scheduledTotal = obligationDtos.Where(o => o.Status != "Cancelled").Sum(o => o.TotalAmount);
    var effectiveTotal = scheduledTotal > 0m ? scheduledTotal : order.TotalAmount;
    var remainingOrder = Math.Max(0m, effectiveTotal - netPaid);

    var hasUnapprovedPackaging = packaging is not null && packaging.Status != CustomPackagingStatus.Approved
        && packaging.Status != CustomPackagingStatus.InProduction && packaging.Status != CustomPackagingStatus.Completed;
    var preProdSatisfied = obligationDtos.Count > 0
        ? obligationDtos.Where(o => o.Status != "Cancelled" && o.IsRequiredBeforeProduction).All(o => o.Status == "Paid")
        : netPaid >= order.TotalAmount && order.TotalAmount > 0m;
    var preShipSatisfied = obligationDtos.Count > 0
        ? obligationDtos.Where(o => o.Status != "Cancelled" && o.IsRequiredBeforeShipping).All(o => o.Status == "Paid")
        : netPaid >= order.TotalAmount && order.TotalAmount > 0m;

    var canStartProduction = order.Status == OrderStatus.Confirmed && !hasUnapprovedPackaging && preProdSatisfied;
    var canMarkShipped = order.Status == OrderStatus.Processing && preShipSatisfied;

    var txDtos = payments.Select(p => new OrderPaymentTransactionItemResponse(
        p.Id, p.PartId, p.PaymentNumber, p.Amount, p.RefundedAmount, p.Method, p.Status,
        p.TransactionId, p.CreatedAt, p.PaidAt)).ToList();

    return new OrderPaymentSummaryResponse(
        order.Id, order.TotalAmount, effectiveTotal, netPaid, totalRefunded, remainingOrder,
        canStartProduction, canMarkShipped, OrderPolicyDefaults.VnPayMaxAmountPerPartVnd,
        obligationDtos, txDtos);
  }

  private static OrderFinancialSummaryResponse BuildFinancialSummary(
      Order order,
      IReadOnlyList<OrderFinancialLedgerEntry> ledger,
      IReadOnlyList<OrderRefundCase> refunds,
      IReadOnlyList<SupplierPayoutRequest> payouts)
  {
    var totals = ComputeBalancesFromLedger(ledger, payouts);
    return new OrderFinancialSummaryResponse(
        order.Id,
        true,
        OrderPolicyDefaults.ProductionPayoutEnabledDefault,
        totals.Captured,
        totals.Held,
        totals.Disputed,
        totals.Refunded,
        totals.Eligible,
        totals.PayoutRequested,
        totals.PaidOutSimulated,
        ledger.Select(l => new OrderFinancialLedgerItemResponse(l.Id, l.EntryType, l.BalanceBucket, l.Amount, l.Description, l.CreatedAt)).ToList(),
        refunds.Select(r => MapRefundCase(r, order.OrderNumber)).ToList(),
        payouts.Select(p => MapPayoutRequest(p, order.OrderNumber)).ToList());
  }

  private static IReadOnlyList<string> ComputeAllowedActions(
      Order order,
      CustomPackagingSummaryResponse? packaging,
      OrderPaymentSummaryResponse paymentSummary,
      OrderFinancialSummaryResponse financialSummary,
      OrderDeliverySnapshotResponse? deliverySnapshot,
      OrderCancellationRequest? pendingCancel,
      OrderDeliveryDispute? activeDispute,
      Guid viewerUserId,
      bool viewerIsSupplier,
      bool viewerIsAdmin)
  {
    var actions = new List<string>();
    var isCustomerOwner = !viewerIsSupplier && !viewerIsAdmin && order.CustomerId == viewerUserId;

    if (isCustomerOwner)
    {
      if (order.Status == OrderStatus.Pending)
        actions.Add("CancelOrder");
      if (order.Status == OrderStatus.AwaitingCustomerDeliveryApproval)
      {
        actions.Add("ApproveDeliveryProposal");
        actions.Add("DeclineDeliveryProposal");
      }
      if (order.Status is OrderStatus.Confirmed or OrderStatus.Processing && paymentSummary.RemainingAmount > 0m)
        actions.Add("PayNextPartVnPay");
      if (order.Status == OrderStatus.Confirmed && paymentSummary.TotalPaidAmount == 0m)
        actions.Add("CancelOrder");
      else if (order.Status is OrderStatus.Confirmed or OrderStatus.Processing && pendingCancel is null)
        actions.Add("RequestCancellation");
      if (pendingCancel is not null && string.Equals(pendingCancel.RequestedByRole, "Supplier", StringComparison.OrdinalIgnoreCase))
        actions.Add("RespondCancellationRequest");
      if (packaging is { Status: CustomPackagingStatus.DesignReview })
        actions.Add("ReviewPackagingMockup");
      if (order.Status == OrderStatus.Shipped && deliverySnapshot is { ProofCount: > 0 })
        actions.Add("ConfirmDeliveryReceipt");
      if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered && activeDispute is null)
        actions.Add("ReportDeliveryDispute");
      if (order.Status is OrderStatus.Cancelled or OrderStatus.Expired && paymentSummary.TotalPaidAmount > financialSummary.RefundedSimulatedAmount)
        actions.Add("RequestRefund");
    }
    else if (viewerIsSupplier)
    {
      if (order.Status == OrderStatus.Pending)
      {
        actions.Add("ConfirmOrder");
        actions.Add("RejectOrder");
        actions.Add("ProposeDeliveryDate");
      }
      if (packaging is { Status: CustomPackagingStatus.Pending or CustomPackagingStatus.RevisionRequested or CustomPackagingStatus.DesignReview })
        actions.Add("SubmitPackagingMockup");
      if (paymentSummary.CanStartProduction)
        actions.Add("StartProduction");
      if (paymentSummary.CanMarkShipped)
        actions.Add("MarkShipped");
      if (order.Status == OrderStatus.Shipped && deliverySnapshot is null)
        actions.Add("CreateShipment");
      if (order.Status == OrderStatus.Shipped && deliverySnapshot is not null && deliverySnapshot.Status != DeliveryStatus.Delivered)
        actions.Add("SubmitDeliveryProof");
      if (order.Status is OrderStatus.Confirmed or OrderStatus.Processing && pendingCancel is null)
        actions.Add("RequestCancellation");
      if (pendingCancel is not null && string.Equals(pendingCancel.RequestedByRole, "Customer", StringComparison.OrdinalIgnoreCase))
        actions.Add("RespondCancellationRequest");
      if (order.Status == OrderStatus.Delivered && financialSummary.EligibleAmount > 0m && activeDispute is null)
        actions.Add("RequestSimulatedPayout");
      if (activeDispute is { Status: "AwaitingSupplierRebuttal" })
        actions.Add("SubmitDeliveryDisputeRebuttal");
    }
    else if (viewerIsAdmin)
    {
      if (activeDispute is not null)
        actions.Add("ResolveDeliveryDispute");
      if (financialSummary.RefundCases.Any(r => r.Status == "Pending"))
        actions.Add("ProcessRefundCase");
      if (financialSummary.PayoutRequests.Any(p => p.Status == "Pending"))
        actions.Add("ProcessSimulatedPayout");
    }

    return actions;
  }

  private Task<Guid> GetSupplierUserIdByShopAsync(Guid shopId, CancellationToken cancellationToken) =>
      (from shop in context.Shops.AsNoTracking()
       join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
       where shop.Id == shopId
       select profile.UserId).FirstOrDefaultAsync(cancellationToken);

  private static ShopOrderPolicyResponse MapShopPolicy(Guid shopId, ShopOrderPolicy? policy, IReadOnlyList<ShopShippingRate> rates) =>
      new(
          shopId,
          policy?.DepositEnabled ?? false,
          policy?.MinimumOrderTotalForDeposit ?? 0m,
          policy?.DepositPercent ?? OrderPolicyDefaults.MinDepositPercent,
          policy?.TaxIncluded ?? true,
          policy?.TaxRate ?? 0m,
          policy?.AllowSelfPickup ?? true,
          policy?.RequireShippingZoneSelection ?? false,
          policy?.Version ?? 1L,
          rates.Select(r => new ShopShippingRateResponse(
              r.Id, r.RegionCode, r.RegionName, r.FeeAmount, r.FreeShippingThreshold, r.EstimatedDays, r.IsSupported)).ToList());

  private static PackagingFeeChangeRequestResponse MapFeeChange(PackagingFeeChangeRequest item) =>
      new(item.Id, item.OrderId, item.RevisionId, item.OldPackagingFee, item.DeltaAmount, item.NewPackagingFee,
          item.OldOrderTotal, item.NewOrderTotal, item.OrderVersionSnapshot, item.Status, item.CreatedAt,
          item.ApprovedAt, item.GeneratedObligationId);

  private static OrderCancellationRequestResponse MapCancelRequest(OrderCancellationRequest item) =>
      new(item.Id, item.OrderId, item.RequestedByUserId, item.RequestedByRole, item.OrderStatusSnapshot,
          item.Reason, item.Status, item.ProposedRefundAmount, item.CreatedAt, item.ReviewedAt, item.ReviewNotes);

  private static OrderRefundCaseResponse MapRefundCase(OrderRefundCase item, string? orderNumber) =>
      new(item.Id, item.OrderId, orderNumber, item.CaseNumber, item.RequestedAmount, item.ApprovedAmount,
          item.Reason, item.Status, item.SandboxSimulated, item.CreatedAt, item.ProcessedAt, item.AdminNotes,
          item.RefundReferenceCode, item.CustomerBankSnapshotJson, item.TransferProofImageUrl);

  private static SupplierPayoutAccountResponse MapPayoutAccount(SupplierPayoutAccount item)
  {
    var raw = item.AccountNumber ?? string.Empty;
    var masked = raw.Length <= 4 ? raw : $"{new string('*', raw.Length - 4)}{raw[^4..]}";
    return new SupplierPayoutAccountResponse(
        item.Id, item.SupplierUserId, item.BankCode, item.BankName, raw, masked,
        item.AccountHolderName, item.BranchName, item.IsVerifiedSimulated, item.Version, item.UpdatedAt);
  }

  private static SupplierPayoutRequestResponse MapPayoutRequest(SupplierPayoutRequest item, string? orderNumber) =>
      new(item.Id, item.OrderId, orderNumber, item.RequestNumber, item.Amount, item.Status,
          item.SandboxSimulated, item.BankSnapshotJson, item.CreatedAt, item.ProcessedAt, item.AdminNotes,
          item.PayoutReferenceCode, item.TransferProofImageUrl);

  private static OrderDeliveryDisputeResponse MapDispute(OrderDeliveryDispute item, string? orderNumber, decimal? paidAmount = null)
  {
    IReadOnlyList<string> urls = [];
    if (!string.IsNullOrWhiteSpace(item.EvidenceUrlsJson))
    {
      try { urls = JsonSerializer.Deserialize<List<string>>(item.EvidenceUrlsJson) ?? []; }
      catch { urls = []; }
    }

    IReadOnlyList<string> rebuttalUrls = [];
    if (!string.IsNullOrWhiteSpace(item.SupplierRebuttalEvidenceUrlsJson))
    {
      try { rebuttalUrls = JsonSerializer.Deserialize<List<string>>(item.SupplierRebuttalEvidenceUrlsJson) ?? []; }
      catch { rebuttalUrls = []; }
    }

    return new OrderDeliveryDisputeResponse(
        item.Id, item.OrderId, orderNumber, item.DeliveryId, item.IssueType, item.Description,
        urls, item.Status, item.ResolutionNotes, item.CreatedAt, item.ResolvedAt,
        item.RefundBankSnapshotJson, paidAmount,
        item.SupplierRebuttalNotes, rebuttalUrls, item.SupplierRebuttalAt, item.RebuttalDeadlineAt);
  }

  private static string EncodeCursor(DateTime createdAtUtc, Guid id)
  {
    var payload = $"{createdAtUtc.ToUniversalTime().Ticks}:{id:N}";
    return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
  }

  private static bool TryDecodeCursor(string? cursor, out DateTime createdAtUtc, out Guid id)
  {
    createdAtUtc = default;
    id = default;
    if (string.IsNullOrWhiteSpace(cursor))
      return false;
    try
    {
      var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor.Trim()));
      var parts = decoded.Split(':', 2);
      if (parts.Length != 2)
        return false;
      if (!long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
        return false;
      if (!Guid.TryParseExact(parts[1], "N", out id))
        return false;
      createdAtUtc = new DateTime(ticks, DateTimeKind.Utc);
      return true;
    }
    catch
    {
      return false;
    }
  }
}
