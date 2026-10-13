namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Removes expired OAuth attempts at regular intervals.</summary>
public class GoogleLoginAttemptCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<GoogleLoginAttemptCleanupService> logger) : BackgroundService
{
    private const string CleanupFailureMessage = "Google login attempt cleanup failed.";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IGoogleAuthRepository>();
                var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
                await repository.CleanupExpiredAttemptsAsync(
                    clock.UtcNow.AddHours(-1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, CleanupFailureMessage);
            }
        }
    }
}
