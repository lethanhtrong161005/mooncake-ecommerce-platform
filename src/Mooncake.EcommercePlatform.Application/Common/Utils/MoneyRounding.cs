namespace Mooncake.EcommercePlatform.Application.Common.Utils;

/// <summary>Central currency rounding policy used by monetary workflows.</summary>
public static class MoneyRounding
{
    public const int CurrencyScale = 2;

    public static decimal RoundCurrency(decimal amount) =>
        decimal.Round(amount, CurrencyScale, MidpointRounding.AwayFromZero);

    public static decimal PercentageOf(decimal amount, decimal percentage) =>
        RoundCurrency(amount * percentage / 100m);
}
