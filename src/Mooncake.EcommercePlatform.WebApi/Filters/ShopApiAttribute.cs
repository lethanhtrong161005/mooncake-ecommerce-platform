namespace Mooncake.EcommercePlatform.WebApi.Filters;

/// <summary>Marks the shop contract and its atomic business audit boundary.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ShopApiAttribute : Attribute;
