namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of shared CRUD operations; domain-specific queries stay in concrete repositories.</summary>
public class GenericRepository<TEntity>(ApplicationDbContext context) : IGenericRepository<TEntity> where TEntity : class
{
    protected ApplicationDbContext Context { get; } = context;

    public async Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default) =>
        await Context.Set<TEntity>().FindAsync([id], cancellationToken);

    public IQueryable<TEntity> Query() => Context.Set<TEntity>();

    public void Add(TEntity entity) => Context.Set<TEntity>().Add(entity);

    public void Update(TEntity entity) => Context.Set<TEntity>().Update(entity);

    public void Remove(TEntity entity) => Context.Set<TEntity>().Remove(entity);
}
