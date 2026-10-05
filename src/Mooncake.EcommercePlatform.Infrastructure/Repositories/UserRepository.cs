namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IUserRepository"/>.</summary>
public class UserRepository(ApplicationDbContext context) : IUserRepository
{
    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Users.AsNoTracking().Where(u => !u.IsDeleted).ToListAsync(cancellationToken);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted, cancellationToken);

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.FindAsync([id], cancellationToken);
        if (user is not null && !user.IsDeleted)
        {
            user.IsDeleted = true;
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
