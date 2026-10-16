using ERP.Domain.Primitives.Pagination;
using System.Linq.Expressions;

namespace ERP.Domain.Repositories;

/// <summary>
/// What every aggregate root can be asked, whatever its key type.
/// <para>
/// Only aggregate roots get a repository. An order line or a document article is reached
/// through the root that owns it, because loading one on its own would let it be changed
/// without the rules the root enforces.
/// </para>
/// </summary>
/// <typeparam name="TEntity">The aggregate root.</typeparam>
/// <typeparam name="TKey">Its key type.</typeparam>
public interface IRepository<TEntity, TKey> where TEntity : IAggregateRoot<TKey> where TKey : IEquatable<TKey>
{
    Task CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the entity as deleted. The row survives so documents that refer to it still resolve.
    /// </summary>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<TEntity?> GetByIdAsync(TKey key, CancellationToken cancellationToken = default);
    Task<bool> IsExistenceByIdAsync(TKey key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetListAsync<T>(Expression<Func<TEntity, T>> expression, CancellationToken cancellationToken = default);
}

/// <summary>
/// The repository every aggregate in this system actually uses: keyed by <see cref="Guid"/>,
/// with the paging, projection and existence checks the application layer needs.
/// </summary>
/// <typeparam name="TEntity">The aggregate root.</typeparam>
public interface IRepository<TEntity> : IRepository<TEntity, Guid> where TEntity : IAggregateRoot<Guid>
{
    Task<TEntity?> GetByIdAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> IsExistenceByIdAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Loads several rows in one round trip rather than one query per identifier.</summary>
    Task<IReadOnlyList<TEntity>> GetByIdsAsync(IEnumerable<Guid> keys, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// A projected, filtered and ordered page of rows, together with the total the filter matched.
    /// Paging belongs in the query rather than in memory, so the database returns only the page.
    /// </summary>
    Task<PaginatedItems<T>> GetPagedAsync<T>(
        IPaginationQuery pagination,
        Expression<Func<TEntity, T>> expression,
        Expression<Func<TEntity, bool>>? where = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool descending = false,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// A short list for a picker or a type ahead box. The caller states how many rows it can
    /// show rather than being given a fixed number it did not ask for.
    /// </summary>
    Task<List<T>> GetForOptionListAsync<T>(
        Expression<Func<TEntity, T>> expression,
        Expression<Func<TEntity, bool>>? where = null,
        Expression<Func<TEntity, object>>? orderby = null,
        int take = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Includes rows that were deleted. Reserved for administration and for reports that must
    /// account for what was removed.
    /// </summary>
    Task<TEntity?> GetByIdIncludingDeletedAsync(Guid key, CancellationToken cancellationToken = default);
}
