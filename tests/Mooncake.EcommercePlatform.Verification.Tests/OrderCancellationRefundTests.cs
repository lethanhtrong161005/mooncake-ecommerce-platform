namespace Mooncake.EcommercePlatform.Verification.Tests;

using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Domain.Enums;
using Xunit;

public sealed class OrderCancellationRefundTests
{
    [Fact]
    public void PolicyDefaults_RefundPercents_AreFiftyAndHundred()
    {
        Assert.Equal(50m, OrderPolicyDefaults.InProductionCancelRefundPercent);
        Assert.Equal(100m, OrderPolicyDefaults.FullRefundPercent);
    }

    [Theory]
    [InlineData(CustomPackagingStatus.Approved)]
    [InlineData(CustomPackagingStatus.InProduction)]
    public void Suggest_ProcessingWithApprovedOrInProductionPackaging_IsFiftyPercent(CustomPackagingStatus packagingStatus)
    {
        var suggestion = OrderRefundSuggestionHelper.Suggest(OrderStatus.Processing, packagingStatus, 1_000_001m);

        Assert.Equal(50m, suggestion.Percent);
        Assert.Equal(500_000.5m, suggestion.Amount);
        Assert.False(string.IsNullOrWhiteSpace(suggestion.Reason));
    }

    [Fact]
    public void Suggest_ProcessingWithoutCustomPackaging_IsFullRefund()
    {
        var suggestion = OrderRefundSuggestionHelper.Suggest(OrderStatus.Processing, null, 2_000_000m);

        Assert.Equal(100m, suggestion.Percent);
        Assert.Equal(2_000_000m, suggestion.Amount);
    }

    [Theory]
    [InlineData(CustomPackagingStatus.Pending)]
    [InlineData(CustomPackagingStatus.DesignReview)]
    [InlineData(CustomPackagingStatus.Rejected)]
    public void Suggest_ProcessingWithUnapprovedPackaging_IsFullRefund(CustomPackagingStatus packagingStatus)
    {
        var suggestion = OrderRefundSuggestionHelper.Suggest(OrderStatus.Processing, packagingStatus, 800_000m);

        Assert.Equal(100m, suggestion.Percent);
        Assert.Equal(800_000m, suggestion.Amount);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.AwaitingCustomerDeliveryApproval)]
    public void Suggest_NonProcessingStatuses_StayFullRefundEvenWithApprovedPackaging(OrderStatus orderStatus)
    {
        var suggestion = OrderRefundSuggestionHelper.Suggest(orderStatus, CustomPackagingStatus.Approved, 600_000m);

        Assert.Equal(100m, suggestion.Percent);
        Assert.Equal(600_000m, suggestion.Amount);
    }

    [Fact]
    public void Suggest_ZeroPaid_IsZero()
    {
        var suggestion = OrderRefundSuggestionHelper.Suggest(OrderStatus.Processing, CustomPackagingStatus.InProduction, 0m);

        Assert.Equal(0m, suggestion.Amount);
    }
}
