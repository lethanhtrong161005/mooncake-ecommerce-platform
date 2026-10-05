namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Net.Mail;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

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
        var admin = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(adminPassword),
            FullName = "Platform Administrator",
            Role = UserRole.Admin,
            IsActive = true,
            EmailVerifiedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        var categories = new List<Category>
        {
            new() { Name = "Traditional Mooncakes", SortOrder = 10, CreatedAt = now, UpdatedAt = now },
            new() { Name = "Snow Skin Mooncakes", SortOrder = 20, CreatedAt = now, UpdatedAt = now },
            new() { Name = "Mooncake Gift Sets", SortOrder = 30, CreatedAt = now, UpdatedAt = now }
        };
        var template = new ShopTemplate
        {
            Name = "Classic",
            Description = "A clean starter layout for supplier shops.",
            CssVariables = "{}",
            LayoutConfig = "{\"sections\":[\"banner\",\"products\"]}",
            IsActive = true,
            SortOrder = 10,
            CreatedAt = now,
            UpdatedAt = now
        };
        return repository.InitializeAsync(admin, categories, template, cancellationToken);
    }
}
