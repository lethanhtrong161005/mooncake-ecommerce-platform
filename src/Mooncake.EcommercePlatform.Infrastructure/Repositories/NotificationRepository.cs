namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Notifications;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

public sealed class NotificationRepository(ApplicationDbContext dbContext) : INotificationRepository
{
    public async Task<NotificationInboxResponse> GetInboxAsync(
        Guid userId, int page, int pageSize, string? category, bool? isRead, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsDeleted);

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var cat = category.ToLowerInvariant();
            query = query.Where(n =>
                (n.Type != null && n.Type.ToLower().Contains(cat)) ||
                (n.ReferenceType != null && n.ReferenceType.ToLower().Contains(cat)));
        }

        var totalItems = await query.CountAsync(ct);
        var unreadCount = await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted, ct);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationItemDto(
                n.Id,
                n.Type ?? "system.notification",
                ResolveCategory(n.Type, n.ReferenceType),
                n.Title ?? "Thông báo mới",
                n.Content ?? string.Empty,
                n.ReferenceId,
                n.ReferenceType,
                ResolveActionUrl(n.ReferenceType, n.ReferenceId),
                ResolvePriority(n.Type),
                n.IsRead,
                n.ReadAtUtc,
                n.CreatedAt
            ))
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        return new NotificationInboxResponse(items, page, pageSize, totalItems, totalPages, unreadCount);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default) =>
        dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted, ct);

    public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId && !n.IsDeleted, ct);

        if (notification == null) return false;

        notification.IsRead = true;
        notification.ReadAtUtc = DateTime.UtcNow;
        notification.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId, string? category, DateTime? readThroughUtc, CancellationToken ct = default)
    {
        var cutoff = readThroughUtc ?? DateTime.UtcNow;
        var query = dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted && n.CreatedAt <= cutoff);

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var cat = category.ToLowerInvariant();
            query = query.Where(n =>
                (n.Type != null && n.Type.ToLower().Contains(cat)) ||
                (n.ReferenceType != null && n.ReferenceType.ToLower().Contains(cat)));
        }

        var unreadNotifications = await query.ToListAsync(ct);
        if (unreadNotifications.Count == 0) return 0;

        var now = DateTime.UtcNow;
        foreach (var notif in unreadNotifications)
        {
            notif.IsRead = true;
            notif.ReadAtUtc = now;
            notif.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(ct);
        return unreadNotifications.Count;
    }

    public async Task<Notification> CreateNotificationAsync(Notification notification, CancellationToken ct = default)
    {
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(ct);
        return notification;
    }

    public async Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId, CancellationToken ct = default)
    {
        var config = await dbContext.SystemConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == $"user_pref_{userId}", ct);

        if (config == null || string.IsNullOrWhiteSpace(config.ValueJson))
        {
            return new NotificationPreferencesDto();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<NotificationPreferencesDto>(config.ValueJson)
                ?? new NotificationPreferencesDto();
        }
        catch
        {
            return new NotificationPreferencesDto();
        }
    }

    public async Task<NotificationPreferencesDto> UpdatePreferencesAsync(Guid userId, NotificationPreferencesDto preferences, CancellationToken ct = default)
    {
        var key = $"user_pref_{userId}";
        var json = System.Text.Json.JsonSerializer.Serialize(preferences);

        var existing = await dbContext.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key, ct);
        if (existing == null)
        {
            dbContext.SystemConfigs.Add(new SystemConfig
            {
                Id = Guid.NewGuid(),
                Key = key,
                ValueJson = json,
                Description = $"Notification preferences for user {userId}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.ValueJson = json;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(ct);
        return preferences;
    }

    private static string ResolveCategory(string? type, string? refType)
    {
        var t = (type ?? string.Empty).ToLowerInvariant();
        var r = (refType ?? string.Empty).ToLowerInvariant();

        if (t.Contains("pay") || r.Contains("pay")) return "payment";
        if (t.Contains("contract") || r.Contains("contract") || t.Contains("milestone")) return "contract";
        if (t.Contains("order") || r.Contains("order") || t.Contains("delivery")) return "order";
        if (t.Contains("rfq") || r.Contains("rfq") || t.Contains("bid")) return "rfq";
        return "system";
    }

    private static string ResolveActionUrl(string? refType, Guid? refId)
    {
        var r = (refType ?? string.Empty).ToLowerInvariant();
        if (refId == null) return "/#/";

        if (r.Contains("contract")) return $"/#/hop-dong-tien-do?id={refId}";
        if (r.Contains("order")) return $"/#/dat-hang-thiet-ke?id={refId}";
        if (r.Contains("delivery")) return $"/#/giao-nhan-pod?id={refId}";
        if (r.Contains("rfq")) return $"/#/rfq-dau-thau?id={refId}";
        return "/#/";
    }

    private static string ResolvePriority(string? type)
    {
        var t = (type ?? string.Empty).ToLowerInvariant();
        if (t.Contains("breach") || t.Contains("dispute") || t.Contains("overdue") || t.Contains("suspend")) return "urgent";
        if (t.Contains("pay") || t.Contains("sign") || t.Contains("pod") || t.Contains("accepted")) return "high";
        return "medium";
    }
}

