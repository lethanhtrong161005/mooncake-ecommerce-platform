namespace Mooncake.EcommercePlatform.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CloudinaryDotNet;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;
using Mooncake.EcommercePlatform.Infrastructure.Services;
using Mooncake.EcommercePlatform.Infrastructure.Options;
using Npgsql;
using Npgsql.NameTranslation;

/// <summary>Registers all Infrastructure-layer services with the DI container.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCloudinaryStorage(configuration);
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DB_CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string is missing. Configure ConnectionStrings:DefaultConnection in appsettings.json or DB_CONNECTION_STRING.");
        }

        // Build NpgsqlDataSource with Enum & Vector mappings (using NpgsqlNullNameTranslator for PascalCase enum matching)
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();

        var translator = new NpgsqlNullNameTranslator();

        dataSourceBuilder.MapEnum<UserRole>("user_role", translator);
        dataSourceBuilder.MapEnum<ShopStatus>("shop_status", translator);
        dataSourceBuilder.MapEnum<ProductStatus>("product_status", translator);
        dataSourceBuilder.MapEnum<OrderStatus>("order_status", translator);
        dataSourceBuilder.MapEnum<PaymentMethod>("payment_method", translator);
        dataSourceBuilder.MapEnum<PaymentStatus>("payment_status", translator);
        dataSourceBuilder.MapEnum<DeliveryType>("delivery_type", translator);
        dataSourceBuilder.MapEnum<DeliveryStatus>("delivery_status", translator);
        dataSourceBuilder.MapEnum<ProofType>("proof_type", translator);
        dataSourceBuilder.MapEnum<CustomPackagingStatus>("custom_packaging_status", translator);
        dataSourceBuilder.MapEnum<RfqStatus>("rfq_status", translator);
        dataSourceBuilder.MapEnum<RfqInvitationStatus>("rfq_invitation_status", translator);
        dataSourceBuilder.MapEnum<BidStatus>("bid_status", translator);
        dataSourceBuilder.MapEnum<ContractStatus>("contract_status", translator);
        dataSourceBuilder.MapEnum<MilestoneStatus>("milestone_status", translator);
        dataSourceBuilder.MapEnum<ReputationEvent>("reputation_event", translator);
        dataSourceBuilder.MapEnum<DiscountType>("discount_type", translator);
        dataSourceBuilder.MapEnum<AiConversationType>("ai_conversation_type", translator);
        dataSourceBuilder.MapEnum<AiMessageRole>("ai_message_role", translator);
        dataSourceBuilder.MapEnum<AiReportType>("ai_report_type", translator);
        dataSourceBuilder.MapEnum<AiReportStatus>("ai_report_status", translator);

        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(dataSource, providerOptions => providerOptions.UseVector());

            // Detailed errors are useful in development; parameter values can contain OAuth secrets.
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableDetailedErrors();

                options.ConfigureWarnings(warnings =>
                {
                    warnings.Log((Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuting, Microsoft.Extensions.Logging.LogLevel.Debug));
                    warnings.Log((Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuted, Microsoft.Extensions.Logging.LogLevel.Debug));
                });
            }
        });

        services.AddHttpContextAccessor();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IGoogleAuthRepository, GoogleAuthRepository>();
        services.AddMemoryCache();
        services.AddHttpClient<IGoogleIdentityClient, GoogleIdentityClient>(client =>
            client.Timeout = TimeSpan.FromSeconds(10));
        services.AddHostedService<GoogleLoginAttemptCleanupService>();
        services.AddScoped<IAdminPlatformRepository, AdminPlatformRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IProductCatalogRepository, ProductCatalogRepository>();
        services.AddScoped<IShopCursorProtector, ShopCursorProtector>();
        services.AddScoped<IFileSafetyScanner, ClamAvFileSafetyScanner>();
        services.AddScoped<IShopMediaGateway, ShopMediaGateway>();
        services.AddHttpClient<IVerificationDocumentGateway, VerificationDocumentGateway>(client =>
            client.Timeout = TimeSpan.FromSeconds(30))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
            .RemoveAllLoggers();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICustomPackagingRepository, CustomPackagingRepository>();
        services.AddScoped<IPlatformBootstrapRepository, PlatformBootstrapRepository>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IRfqRepository, RfqRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IDeliveryRepository, DeliveryRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IAiConversationRepository, AiConversationRepository>();
        services.AddScoped<IAiAnalyticsReportRepository, AiAnalyticsReportRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
        services.AddHttpClient<IGeminiGateway, GeminiGateway>();
        services.AddOptions<VnPayOptions>().Bind(configuration.GetSection(VnPayOptions.SectionName))
            .Validate(options => options.IsValid(), "VNPay configuration is invalid.");
        services.AddSingleton<IVnPayGateway, VnPayGateway>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IAccessTokenService, JwtAccessTokenService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ITraceContext, TraceContext>();

        return services;
    }

    /// <summary>Registers upload-only storage without database or network access during configuration.</summary>
    public static IServiceCollection AddCloudinaryStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CloudinaryOptions>()
            .Bind(configuration.GetSection(CloudinaryOptions.SectionName))
            .Validate(options => options.IsValid(), CloudinaryOptions.InvalidConfigurationMessage)
            .ValidateOnStart();
        services.AddSingleton<ICloudinaryUploadApi>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<CloudinaryOptions>>().Value;
            var client = new Cloudinary(new Account(options.CloudName, options.ApiKey, options.ApiSecret));
            client.Api.Secure = options.Secure;
            client.Api.Timeout = checked(options.UploadTimeoutSeconds * 1000);
            return client;
        });
        services.AddScoped<ICloudinaryStorageService, CloudinaryStorageService>();
        return services;
    }
}
