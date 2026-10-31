namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Net.Mail;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Admin;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Creates the initial admin account and minimum catalog data on explicit operator request.</summary>
public sealed class PlatformBootstrapService(
    IPlatformBootstrapRepository repository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider) : IPlatformBootstrapService
{
    public Task InitializeAsync(string adminEmail, string adminPassword, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = adminEmail.Trim().ToLowerInvariant();
        try
        {
            _ = new MailAddress(normalizedEmail);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("INITIAL_ADMIN_EMAIL must be a valid email address.");
        }

        if (adminPassword.Length < 12)
            throw new InvalidOperationException("INITIAL_ADMIN_PASSWORD must contain at least 12 characters.");

        var now = dateTimeProvider.UtcNow;
        var data = new PlatformBootstrapData(normalizedEmail, passwordHasher.Hash(adminPassword), "Platform Administrator", now,
            [("Traditional Mooncakes", 10), ("Snow Skin Mooncakes", 20), ("Mooncake Gift Sets", 30)],
            "Classic", "A clean starter layout for supplier shops.", "{}", "{\"sections\":[\"banner\",\"products\"]}", 10);
        return repository.InitializeAsync(data, cancellationToken);
    }
}
