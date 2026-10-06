namespace Mooncake.EcommercePlatform.WebApi.Background;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>Expires stale authentication codes and refresh sessions.</summary>
public sealed class AuthMaintenanceJob(IServiceScopeFactory scopeFactory) : IRecurringJob
{
    public string Name => "Authentication maintenance";
    public TimeSpan Interval => TimeSpan.FromHours(1);

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        await dbContext.Users.Where(user => user.EmailVerificationCodeExpiresAt <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.EmailVerificationCodeHash, (string?)null)
                .SetProperty(user => user.EmailVerificationCodeExpiresAt, (DateTime?)null)
                .SetProperty(user => user.EmailVerificationCodeAttempts, 0), cancellationToken);
        await dbContext.Users.Where(user => user.PasswordResetCodeExpiresAt <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.PasswordResetCodeHash, (string?)null)
                .SetProperty(user => user.PasswordResetCodeExpiresAt, (DateTime?)null)
                .SetProperty(user => user.PasswordResetCodeAttempts, 0), cancellationToken);
        await dbContext.RefreshSessions.Where(session => session.ExpiresAt <= now && session.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAt, now), cancellationToken);
    }
}
