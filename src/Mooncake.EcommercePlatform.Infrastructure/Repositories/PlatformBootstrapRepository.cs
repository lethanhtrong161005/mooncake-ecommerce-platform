namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Admin;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of explicit marketplace bootstrap.</summary>
public sealed class PlatformBootstrapRepository(ApplicationDbContext context) : GenericRepository<User>(context), IPlatformBootstrapRepository
{
    public async Task InitializeAsync(PlatformBootstrapData data, CancellationToken cancellationToken = default)
    {
        await Context.Database.MigrateAsync(cancellationToken);
        await using var transaction = await Context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        if (await Context.Users.AnyAsync(user => user.Role == UserRole.Admin && !user.IsDeleted, cancellationToken))
            throw new InvalidOperationException("An active admin account already exists; bootstrap was stopped without changing user accounts.");
        if (await Context.Users.AnyAsync(user => user.Email.ToLower() == data.Email && !user.IsDeleted, cancellationToken))
            throw new InvalidOperationException("INITIAL_ADMIN_EMAIL already belongs to an account; use a different address.");

        var now = data.VerifiedAtUtc;
        var initialAdmin = new User
        {
            Email = data.Email, PasswordHash = data.PasswordHash, FullName = data.FullName, Role = UserRole.Admin,
            IsActive = true, EmailVerifiedAt = now, CreatedAt = now, UpdatedAt = now
        };
        Context.Users.Add(initialAdmin);
        foreach (var categoryData in data.DefaultCategories)
        {
            var exists = await Context.Categories.AnyAsync(item => item.Name.ToLower() == categoryData.Name.ToLower() && !item.IsDeleted, cancellationToken);
            if (!exists)
                Context.Categories.Add(new Category { Name = categoryData.Name, SortOrder = categoryData.SortOrder, CreatedAt = now, UpdatedAt = now });
        }

        var hasActiveTemplate = await Context.ShopTemplates.AnyAsync(template => template.IsActive && !template.IsDeleted, cancellationToken);
        if (!hasActiveTemplate)
            Context.ShopTemplates.Add(new ShopTemplate
            {
                Name = data.TemplateName, Description = data.TemplateDescription, CssVariables = data.CssVariables,
                LayoutConfig = data.LayoutConfig, IsActive = true, SortOrder = data.TemplateSortOrder,
                CreatedAt = now, UpdatedAt = now
            });

        await Context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
