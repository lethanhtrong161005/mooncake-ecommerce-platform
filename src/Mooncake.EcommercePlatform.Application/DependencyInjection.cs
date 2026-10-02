namespace Mooncake.EcommercePlatform.Application;

using Microsoft.Extensions.DependencyInjection;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Registers all Application-layer services with the DI container.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ── Helpers ──
        services.AddSingleton<IUserHelper, UserHelper>();
        services.AddSingleton<ISupplierHelper, SupplierHelper>();
        services.AddSingleton<IShopTemplateHelper, ShopTemplateHelper>();
        services.AddSingleton<ICategoryHelper, CategoryHelper>();
        services.AddSingleton<IPromotionRuleHelper, PromotionRuleHelper>();
        services.AddSingleton<IProductHelper, ProductHelper>();
        services.AddSingleton<IShopHelper, ShopHelper>();
        services.AddSingleton<ICustomPackagingHelper, CustomPackagingHelper>();
        services.AddSingleton<INotificationHelper, NotificationHelper>();
        services.AddSingleton<IAiHelper, AiHelper>();
        services.AddSingleton<IOrderHelper, OrderHelper>();
        services.AddSingleton<IRfqHelper, RfqHelper>();
        services.AddSingleton<IContractHelper, ContractHelper>();
        services.AddSingleton<IPaymentHelper, PaymentHelper>();
        services.AddSingleton<IReviewHelper, ReviewHelper>();
        services.AddSingleton<IDeliveryHelper, DeliveryHelper>();

        // ── Services (TV1: Platform & Accounts) ──
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAiService, AiService>();

        // ── Services (TV2: Shop & Catalog) ──
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IPromotionRuleService, PromotionRuleService>();

        // ── Services (TV3: Cart & Orders) ──
        services.AddScoped<ICustomPackagingService, CustomPackagingService>();
        services.AddScoped<IOrderService, OrderService>();

        // ── Services (TV4: RFQ & Quotations) ──
        services.AddScoped<IRfqService, RfqService>();

        // ── Services (TV5: Contracts & Payments) ──
        services.AddScoped<IContractService, ContractService>();
        services.AddScoped<IPaymentService, PaymentService>();

        // ── Services (TV6: Reviews, Reputation & Delivery) ──
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IDeliveryService, DeliveryService>();

        return services;
    }
}
