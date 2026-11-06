namespace Mooncake.EcommercePlatform.WebApi.Filters;

/// <summary>Marks bulk order, payment, packaging, and delivery endpoints for structured validation responses.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class OrderApiAttribute : Attribute;

