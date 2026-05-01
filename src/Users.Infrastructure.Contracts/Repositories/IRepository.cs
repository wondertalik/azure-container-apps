namespace Users.Infrastructure.Contracts.Repositories;

public interface IRepository<TEntity, in TKey>
{
    Task<TEntity?> GetAsync(TKey id, CancellationToken cancellationToken);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken);
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken);
    Task DeleteAsync(TKey id, Guid deletedBy, CancellationToken cancellationToken);
}
