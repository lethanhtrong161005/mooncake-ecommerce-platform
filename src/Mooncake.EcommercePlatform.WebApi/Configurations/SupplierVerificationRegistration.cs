namespace Mooncake.EcommercePlatform.WebApi.Configurations;

using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;
using Mooncake.EcommercePlatform.Infrastructure.Services;
using Mooncake.EcommercePlatform.WebApi.Background;
using Mooncake.EcommercePlatform.WebApi.Realtime;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants;

/// <summary>Feature wiring, guarded scale-out and purpose-separated read-all tokens.</summary>
public static class SupplierVerificationRegistration
{
    public static IServiceCollection AddSupplierVerificationFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IVerificationNotificationRepository, VerificationNotificationRepository>();
        services.AddScoped<IVerificationNotificationService, VerificationNotificationService>();
        services.AddScoped<IVerificationReadTokenProtector, VerificationReadTokenProtector>();
        services.AddScoped<IVerificationRealtimePublisher, VerificationRealtimePublisher>();
        services.AddScoped<IRealtimeNotificationPublisher, SignalRNotificationPublisher>();
        services.AddSingleton<IUserIdProvider, VerificationUserIdProvider>();
        services.AddHostedService<VerificationOutboxWorker>();
        services.AddAuthorization(options => options.AddPolicy("SupplierExistingFulfillment",
            policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
                context.User.IsInRole("Supplier") || context.User.HasClaim("supplier_fulfillment", "true"))));
        var protection = services.AddDataProtection().SetApplicationName("Mooncake.SupplierVerification");
        var keyRing = configuration["SupplierVerification:DataProtectionKeyDirectory"];
        if (!string.IsNullOrWhiteSpace(keyRing)) protection.PersistKeysToFileSystem(new DirectoryInfo(keyRing));
        var redis = configuration["SupplierVerification:RedisConnection"];
        if (configuration.GetValue<int>("SupplierVerification:InstanceCount") > 1 &&
            (string.IsNullOrWhiteSpace(redis) || string.IsNullOrWhiteSpace(keyRing)))
            throw new InvalidOperationException(ScaleOutRequired);
        var signalR = services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = false;
            options.MaximumReceiveMessageSize = 4096;
        });
        if (!string.IsNullOrWhiteSpace(redis)) signalR.AddStackExchangeRedis(redis, options =>
            options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("mooncake-verification"));
        services.AddRateLimiter(options => options.AddPolicy(C.RatePolicy, context =>
            RateLimitPartition.GetFixedWindowLimiter(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
                })));
        return services;
    }

    private const string ScaleOutRequired = "Multiple verification instances require a Redis backplane and shared protected Data Protection key directory.";
}
