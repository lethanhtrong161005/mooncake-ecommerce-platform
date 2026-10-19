namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Explicit, one-time database bootstrap operations.</summary>
public interface IPlatformBootstrapRepository
{
    Task InitializeAsync(User initialAdmin, IReadOnlyList<Category> defaultCategories,
        IReadOnlyList<ShopTemplate> defaultTemplates, CancellationToken cancellationToken = default);
}
