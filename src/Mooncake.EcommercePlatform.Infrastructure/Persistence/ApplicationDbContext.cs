namespace Mooncake.EcommercePlatform.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Primary EF Core DbContext for the Mooncake platform.</summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
