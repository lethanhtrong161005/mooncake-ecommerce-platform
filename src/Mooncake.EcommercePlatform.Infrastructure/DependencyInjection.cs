namespace Mooncake.EcommercePlatform.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;
using Mooncake.EcommercePlatform.Infrastructure.Services;

/// <summary>Registers all Infrastructure-layer services and repositories with the DI container.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var dbHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
        var dbPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
        var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "mooncake";
        var dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "mooncake_user";
        var dbPass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "mooncake_dev_2026";

        var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass};";

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);

            // Print SQL parameters and detailed errors when debugging/development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();

                options.ConfigureWarnings(warnings =>
                {
                    warnings.Log((Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuting, Microsoft.Extensions.Logging.LogLevel.Debug));
                    warnings.Log((Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuted, Microsoft.Extensions.Logging.LogLevel.Debug));
                });
            }
        });

        services.AddHttpContextAccessor();

        // ── Shared Infrastructure Services ──
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ITraceContext, TraceContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<ISeedService, SeedService>();

        // ── Repositories (TV1: Platform & Accounts) ──
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IShopTemplateRepository, ShopTemplateRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAiRepository, AiRepository>();

        // ── Repositories (TV2: Shop & Catalog) ──
        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPromotionRuleRepository, PromotionRuleRepository>();

        // ── Repositories (TV3: Cart & Orders) ──
        services.AddScoped<ICustomPackagingRepository, CustomPackagingRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        // ── Repositories (TV4: RFQ & Quotations) ──
        services.AddScoped<IRfqRepository, RfqRepository>();

        // ── Repositories (TV5: Contracts & Payments) ──
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        // ── Repositories (TV6: Reviews, Reputation & Delivery) ──
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReputationLogRepository, ReputationLogRepository>();
        services.AddScoped<IDeliveryRepository, DeliveryRepository>();

        return services;
    }
}
