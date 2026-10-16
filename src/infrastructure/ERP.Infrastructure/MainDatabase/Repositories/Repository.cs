using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Primitives.Pagination;
using ERP.Domain.Repositories;
using System.Linq.Expressions;

namespace ERP.Infrastructure.MainDatabase.Repositories
{
    public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
    {
        protected readonly IMainDbContext _context;
        protected readonly DbSet<TEntity> _entity;

        protected Repository(IMainDbContext context)
        {
            _context = context;
            _entity = _context.SetEntity<TEntity>();
        }

        /// <summary>
        /// A read only query. Nothing that is only going to be projected and returned needs to be
        /// tracked, and tracking it would cost a snapshot of every row.
        /// </summary>
        protected IQueryable<TEntity> Query => _entity.AsNoTracking();

        /// <summary>A tracked query, for rows that are about to be changed.</summary>
        protected IQueryable<TEntity> TrackedQuery => _entity.AsQueryable();

        public async Task CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _entity.AddAsync(entity, cancellationToken);
        }

        public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _entity.Update(entity);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Marks the entity as deleted rather than removing the row.
        /// <para>
        /// A physical delete would break every document that refers to it, and in a system that
        /// keeps financial history that reference has to survive. The global query filter on
        /// <c>IsDeleted</c> is what keeps the row out of ordinary queries afterwards.
        /// </para>
        /// </summary>
        public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity, nameof(entity));
            entity.Delete();
            _entity.Update(entity);
            return Task.CompletedTask;
        }

        public async Task<TEntity?> GetByIdAsync(Guid key, CancellationToken cancellationToken = default)
        {
            var result = await _entity.SingleOrDefaultAsync(e => e.Id == key, cancellationToken);
            return result;
        }

        public async Task<bool> IsExistenceByIdAsync(Guid key, CancellationToken cancellationToken = default)
        {
            var result = await _entity.AnyAsync(e => e.Id == key, cancellationToken);
            return result;
        }

        public async Task<IReadOnlyList<T>> GetListAsync<T>(Expression<Func<TEntity, T>> expression, CancellationToken cancellationToken = default)
        {
            var result = await Query.Select(expression).ToListAsync(cancellationToken);
            return result;
        }

        /// <summary>
        /// The identifier is parsed before the query runs so the primary key index can be used.
        /// Comparing <c>Id.ToString() == key</c> makes the database convert every row and scan the
        /// whole table. A value that is not a identifier matches nothing, so it returns null.
        /// </summary>
        public async Task<TEntity?> GetByIdAsync(string key, CancellationToken cancellationToken = default)
        {
            return Guid.TryParse(key, out var id)
                ? await GetByIdAsync(id, cancellationToken)
                : null;
        }

        public async Task<bool> IsExistenceByIdAsync(string key, CancellationToken cancellationToken = default)
        {
            return Guid.TryParse(key, out var id)
                   && await IsExistenceByIdAsync(id, cancellationToken);
        }

        public async Task<IReadOnlyList<TEntity>> GetByIdsAsync(IEnumerable<Guid> keys, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(keys, nameof(keys));

            var distinct = keys.Distinct().ToArray();
            if (distinct.Length == 0)
                return [];

            return await _entity.Where(e => distinct.Contains(e.Id)).ToListAsync(cancellationToken);
        }

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));
            return await Query.AnyAsync(predicate, cancellationToken);
        }

        public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            return predicate is null
                ? await Query.CountAsync(cancellationToken)
                : await Query.CountAsync(predicate, cancellationToken);
        }

        public async Task<PaginatedItems<T>> GetPagedAsync<T>(
            IPaginationQuery pagination,
            Expression<Func<TEntity, T>> expression,
            Expression<Func<TEntity, bool>>? where = null,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool descending = false,
            CancellationToken cancellationToken = default) where T : class
        {
            ArgumentNullException.ThrowIfNull(pagination, nameof(pagination));
            ArgumentNullException.ThrowIfNull(expression, nameof(expression));

            var query = Query;
            if (where is not null)
                query = query.Where(where);

            // Counted before paging so the caller learns how many rows the filter matched, not how
            // many came back on this page.
            var totalCount = await query.LongCountAsync(cancellationToken);
            if (totalCount == 0)
                return PaginatedItems<T>.Empty(pagination.PageNumber, pagination.PageSize);

            if (orderBy is not null)
                query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
            else
                // A page without an order is not reproducible, so fall back to a stable column.
                query = query.OrderByDescending(e => e.CreationDateTime);

            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(expression)
                .ToListAsync(cancellationToken);

            return new PaginatedItems<T>(pagination.PageNumber, pagination.PageSize, totalCount, items);
        }

        public async Task<List<T>> GetForOptionListAsync<T>(
            Expression<Func<TEntity, T>> expression,
            Expression<Func<TEntity, bool>>? where = null,
            Expression<Func<TEntity, object>>? orderby = null,
            int take = 10,
            CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take, nameof(take));

            var query = Query;
            if (where is not null)
            {
                query = query.Where(where);
            }
            if (orderby is not null)
            {
                query = query.OrderBy(orderby);
            }
            var result = await query.Select(expression).Take(take).ToListAsync(cancellationToken);
            return result;
        }

        public async Task<TEntity?> GetByIdIncludingDeletedAsync(Guid key, CancellationToken cancellationToken = default)
        {
            return await _entity.IgnoreQueryFilters().SingleOrDefaultAsync(e => e.Id == key, cancellationToken);
        }
    }
}
