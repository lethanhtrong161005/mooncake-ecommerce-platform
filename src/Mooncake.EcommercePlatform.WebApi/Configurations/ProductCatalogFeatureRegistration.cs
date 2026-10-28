namespace Mooncake.EcommercePlatform.WebApi.Configurations;

using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ProductConstants;

public static class ProductCatalogFeatureRegistration
{
    public static IServiceCollection AddProductCatalogFeature(this IServiceCollection services)
    {
        services.AddHostedService<Mooncake.EcommercePlatform.WebApi.Background.ProductSearchProjectionWorker>();
        services.AddRateLimiter(options => options.AddPolicy(C.RatePolicy, context =>
            RateLimitPartition.GetFixedWindowLimiter(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                })));
        return services;
    }
}
