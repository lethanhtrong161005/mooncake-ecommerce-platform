namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Computes the default refund suggestion for an order cancellation (decision D24).</summary>
public static class OrderRefundSuggestionHelper
{
    private const string FullRefundReason = "Full refund of the paid amount.";
    private const string InProductionReason = "Order is in production with approved custom packaging: 50% of the paid amount is suggested.";

    /// <summary>Immutable refund suggestion result.</summary>
    public sealed record RefundSuggestion(decimal Amount, decimal Percent, string Reason);

    /// <summary>
    /// Suggests the refund amount: 50% for a Processing order whose custom packaging is approved or in production,
    /// otherwise 100% of the paid amount. Admin can still adjust the final refund amount.
    /// </summary>
    public static RefundSuggestion Suggest(OrderStatus orderStatus, CustomPackagingStatus? packagingStatus, decimal paidAmount)
    {
        var inProduction = orderStatus == OrderStatus.Processing
            && packagingStatus is CustomPackagingStatus.Approved or CustomPackagingStatus.InProduction;

        var percent = inProduction
            ? OrderPolicyDefaults.InProductionCancelRefundPercent
            : OrderPolicyDefaults.FullRefundPercent;
        var amount = decimal.Round(paidAmount * percent / 100m, 2, MidpointRounding.AwayFromZero);
        return new RefundSuggestion(amount, percent, inProduction ? InProductionReason : FullRefundReason);
    }
}
