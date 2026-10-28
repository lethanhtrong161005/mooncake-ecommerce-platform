namespace Mooncake.EcommercePlatform.WebApi.Configurations;

using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.WebApi.Filters;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;

public static class ShopFeatureRegistration
{
    public static IServiceCollection AddShopFeature(this IServiceCollection services)
    {
        services.AddHostedService<Mooncake.EcommercePlatform.WebApi.Background.ShopMediaCleanupWorker>();
        services.Configure<MvcOptions>(options => options.Filters.Add<ShopRequestGuard>());
        services.AddRateLimiter(options => options.AddPolicy(C.RatePolicy, context =>
            RateLimitPartition.GetFixedWindowLimiter(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions {
                    PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true })));
        return services;
    }
}
