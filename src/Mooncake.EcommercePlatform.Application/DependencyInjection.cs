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
        services.AddSingleton<IUserHelper, UserHelper>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
