namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of explicit marketplace bootstrap.</summary>
public sealed class PlatformBootstrapRepository(ApplicationDbContext context) : IPlatformBootstrapRepository
{
    public async Task InitializeAsync(User initialAdmin, IReadOnlyList<Category> defaultCategories, ShopTemplate defaultTemplate, CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        if (await context.Users.AnyAsync(user => user.Role == UserRole.Admin && !user.IsDeleted, cancellationToken))
            throw new InvalidOperationException("An active admin account already exists; bootstrap was stopped without changing user accounts.");
        if (await context.Users.AnyAsync(user => user.Email.ToLower() == initialAdmin.Email && !user.IsDeleted, cancellationToken))
            throw new InvalidOperationException("INITIAL_ADMIN_EMAIL already belongs to an account; use a different address.");

        context.Users.Add(initialAdmin);
        foreach (var category in defaultCategories)
        {
            var exists = await context.Categories.AnyAsync(item => item.Name.ToLower() == category.Name.ToLower() && !item.IsDeleted, cancellationToken);
            if (!exists)
                context.Categories.Add(category);
        }

        var hasActiveTemplate = await context.ShopTemplates.AnyAsync(template => template.IsActive && !template.IsDeleted, cancellationToken);
        if (!hasActiveTemplate)
            context.ShopTemplates.Add(defaultTemplate);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
