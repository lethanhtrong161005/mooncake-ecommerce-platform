namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Shared persistence operations for aggregate roots with standard CRUD needs.</summary>
public interface IGenericRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
    IQueryable<TEntity> Query();
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
