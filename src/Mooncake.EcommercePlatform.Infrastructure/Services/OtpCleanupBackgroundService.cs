namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

public class OtpCleanupBackgroundService(
    IServiceScopeFactory scopeFactory, ILogger<OtpCleanupBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var now = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>().UtcNow;
                    await context.OtpVerifications.Where(x =>
                        x.ExpiresAtUtc < now.AddHours(-1) &&
                        (x.ConsumedAtUtc == null || x.ConsumedAtUtc < now.AddHours(-24)))
                        .ExecuteDeleteAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "OTP cleanup failed.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
