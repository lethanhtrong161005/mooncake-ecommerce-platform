namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Utils;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core repository for driver profiles and trip persistence operations.</summary>
public class DriverRepository(ApplicationDbContext context) : IDriverRepository
{
    public Task<DriverProfile?> GetProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.DriverProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted, cancellationToken);

    public Task<DriverProfile?> GetProfileByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        var normalized = PhoneNumberUtils.Normalize(phone);
        return context.DriverProfiles
            .FirstOrDefaultAsync(p => p.Phone == normalized && !p.IsDeleted, cancellationToken);
    }

    public async Task<DriverProfile> CreateProfileAsync(DriverProfile profile, CancellationToken cancellationToken = default)
    {
        context.DriverProfiles.Add(profile);
        await context.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<DriverProfile> UpdateProfileAsync(DriverProfile profile, CancellationToken cancellationToken = default)
    {
        context.DriverProfiles.Update(profile);
        await context.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<PagedResult<Delivery>> GetDriverTripsPageAsync(
        string phone, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = PhoneNumberUtils.Normalize(phone);
        var query = context.Deliveries
            .AsNoTracking()
            .Where(d => !d.IsDeleted && d.DriverPhone == normalizedPhone);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim().ToLowerInvariant();
            if (st is "chờ nhận" or "cho nhan" or "pending" or "unclaimed")
            {
                query = query.Where(d => d.DriverClaimedAt == null || d.Status == DeliveryStatus.Pending);
            }
            else if (st is "đang giao" or "dang giao" or "in_transit" or "intransit" or "delivering")
            {
                query = query.Where(d => d.DriverClaimedAt != null
                    && d.Status != DeliveryStatus.Delivered
                    && d.Status != DeliveryStatus.Returned
                    && d.Status != DeliveryStatus.Failed);
            }
            else if (st is "đã giao" or "da giao" or "delivered")
            {
                query = query.Where(d => d.Status == DeliveryStatus.Delivered);
            }
            else if (Enum.TryParse<DeliveryStatus>(status.Trim(), true, out var parsedEnum))
            {
                query = query.Where(d => d.Status == parsedEnum);
            }
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Delivery>(items, page, pageSize, total);
    }
}
