namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Text.Json;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Admin;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed class AdminPlatformService(IAdminPlatformRepository repository, IUserRepository userRepository, IDateTimeProvider dateTimeProvider) : IAdminPlatformService
{
    private const string ConfigAction = "SystemConfigUpdated";
    private const string UserAction = "UserUpdatedByAdmin";

    public async Task<AdminDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var metrics = await repository.GetMetricsAsync(cancellationToken);
        return new(metrics.TotalUsers, metrics.ActiveUsers, metrics.PendingSuppliers, metrics.ActiveShops,
            metrics.TotalOrders, metrics.PaidGmv, metrics.PendingPayments, metrics.DisputedContracts,
            metrics.TopSuppliers.Select(item => new AdminTopSupplierResponse(item.SupplierId, item.CompanyName, item.SalesAmount)).ToList(),
            metrics.TopProducts.Select(item => new AdminTopProductResponse(item.ProductId, item.ProductName, item.UnitsSold, item.SalesAmount)).ToList());
    }

    public async Task<IReadOnlyList<SystemConfigResponse>> GetSystemConfigsAsync(CancellationToken cancellationToken = default) =>
        (await repository.GetSystemConfigsAsync(cancellationToken))
        .Select(config => new SystemConfigResponse(config.Key, config.ValueJson, config.Description, config.UpdatedAtUtc)).ToList();

    public async Task<SystemConfigResponse> SetSystemConfigAsync(string key, string valueJson, string? description, Guid actorId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 120) throw new HttpException(400, "Configuration key must contain 1 to 120 characters.");
        try { using var _ = JsonDocument.Parse(valueJson); }
        catch (JsonException) { throw new HttpException(400, "Configuration value must be valid JSON."); }

        var config = await repository.GetSystemConfigAsync(key.Trim(), cancellationToken);
        var now = dateTimeProvider.UtcNow;
        if (config is null) config = new SystemConfig { Key = key.Trim(), CreatedAt = now };
        config.ValueJson = valueJson;
        config.Description = description?.Trim();
        config.UpdatedAt = now;
        await repository.SaveSystemConfigAsync(config, cancellationToken);
        await repository.SaveAuditAsync(new AuditLog
        {
            ActorUserId = actorId, Action = ConfigAction, EntityName = nameof(SystemConfig), EntityId = config.Id.ToString(),
            MetadataJson = JsonSerializer.Serialize(new { config.Key, config.ValueJson }), IpAddress = ipAddress, CreatedAt = now
        }, cancellationToken);
        return new(config.Key, config.ValueJson, config.Description, config.UpdatedAtUtc);
    }

    public async Task<IReadOnlyList<AuditLogResponse>> GetAuditLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 200) throw new HttpException(400, "Page must be positive and page size must be between 1 and 200.");
        return (await repository.GetAuditLogsAsync(page, pageSize, cancellationToken))
            .Select(log => new AuditLogResponse(log.Id, log.ActorUserId, log.Action, log.EntityName, log.EntityId,
                log.MetadataJson, log.IpAddress, log.CreatedAtUtc)).ToList();
    }

    public async Task UpdateUserAsync(Guid userId, AdminUserUpdateRequest request, Guid actorId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (request.IsActive is null && request.Role is null) throw new HttpException(400, "At least one user field must be supplied.");
        if (request.Role is UserRole suppliedRole && !Enum.IsDefined(suppliedRole)) throw new HttpException(400, "Role value is invalid.");
        if (request.Role == UserRole.Admin) throw new HttpException(400, "Admin role cannot be assigned through this endpoint.");
        var user = await repository.GetUserForUpdateAsync(userId, cancellationToken) ?? throw new HttpException(404, "User was not found.");
        if (user.Role == UserRole.Admin) throw new HttpException(409, "Administrative accounts cannot be changed through this endpoint.");
        if (request.Role == UserRole.Supplier && !await repository.IsSupplierVerifiedAsync(userId, cancellationToken))
            throw new HttpException(409, "Only verified supplier accounts can be assigned the Supplier role.");
        var previous = new { user.IsActive, user.Role };
        if (request.IsActive is bool active) user.IsActive = active;
        if (request.Role is UserRole role) user.Role = role;
        user.UpdatedBy = actorId;
        user.UpdatedAt = dateTimeProvider.UtcNow;
        var audit = new AuditLog
        {
            ActorUserId = actorId, Action = UserAction, EntityName = nameof(User), EntityId = userId.ToString(),
            MetadataJson = JsonSerializer.Serialize(new { Before = previous, After = new { user.IsActive, user.Role } }),
            IpAddress = ipAddress, CreatedAt = dateTimeProvider.UtcNow
        };
        await repository.SaveUserAndAuditAsync(user, audit, cancellationToken);
        if (request.IsActive is false)
            await userRepository.RevokeAllRefreshSessionsAsync(userId, dateTimeProvider.UtcNow, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminShopResponse>> GetShopsAsync(CancellationToken cancellationToken = default)
    {
        var shops = await repository.GetShopsAsync(cancellationToken);
        var results = new List<AdminShopResponse>(shops.Count);
        foreach (var shop in shops)
        {
            var response = new Mooncake.EcommercePlatform.Application.DTOs.Catalog.Responses.ShopResponse(
                shop.Id, shop.SupplierId, shop.Name, shop.Slug, shop.Description, shop.BannerUrl, shop.LogoUrl, shop.Status);
            results.Add(new(response, await repository.GetShopProductCountAsync(shop.Id, cancellationToken)));
        }
        return results;
    }

    public async Task SetShopStatusAsync(Guid shopId, ShopStatus status, CancellationToken cancellationToken = default)
    {
        if (status is not (ShopStatus.Active or ShopStatus.Inactive or ShopStatus.Suspended))
            throw new HttpException(400, "Shop status is invalid.");
        var shop = await repository.GetShopForUpdateAsync(shopId, cancellationToken) ?? throw new HttpException(404, "Shop was not found.");
        shop.Status = status;
        shop.UpdatedAt = dateTimeProvider.UtcNow;
        await repository.SaveShopAsync(shop, cancellationToken);
    }
}
