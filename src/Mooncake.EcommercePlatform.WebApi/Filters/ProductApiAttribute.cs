namespace Mooncake.EcommercePlatform.WebApi.Filters;

/// <summary>Marks product catalog endpoints for feature-scoped validation responses.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ProductApiAttribute : Attribute;
