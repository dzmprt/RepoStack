using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace RepoStack.EntityFrameworkCore;

public class BaseProvider<TEntity> : IProvider<TEntity> where TEntity : class
{
    protected readonly DbContext DbContext;

    protected readonly DbSet<TEntity> DbSet;

    protected readonly bool AsNoTracing;

    public BaseProvider(DbContext dbContext, bool useAsNoTracing = true)
    {
        DbContext = dbContext;
        DbSet = DbContext.Set<TEntity>();
        AsNoTracing = useAsNoTracing;
    }

    public virtual async ValueTask<long> CountAsync(IQuery<TEntity> specQuery, CancellationToken cancellationToken)
    {
        var query = CreateQueryByQuery(specQuery);
        return await query.LongCountAsync(cancellationToken);
    }

    public virtual ValueTask<long> CountAsync(Expression<Func<TEntity, bool>>? predicate,
        CancellationToken cancellationToken) => CountAsync(new Query<TEntity>(predicate), cancellationToken);

    public virtual async ValueTask<long> CountAsync<TResult>(IQuery<TEntity, TResult> specQuery, CancellationToken cancellationToken)
    {
        var query = CreateQueryByQueryWithProjection(specQuery);
        return await query.LongCountAsync(cancellationToken);
    }

    public virtual async ValueTask<long> CountAsync(CancellationToken cancellationToken)
    {
        var queryable = DbSet.AsQueryable();
        return await queryable.LongCountAsync(cancellationToken);
    }

    public virtual async ValueTask<TResult?> FirstOrDefaultAsync<TResult>(IQuery<TEntity, TResult> specQuery, CancellationToken cancellationToken)
    {
        var query = CreateQueryByQueryWithProjection(specQuery);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async ValueTask<TEntity?> FirstOrDefaultAsync(IQuery<TEntity> specQuery, CancellationToken cancellationToken)
    {
        var query = CreateQueryByQuery(specQuery);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public virtual ValueTask<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate,
        IQueryOrder<TEntity>[]? orderings, int? limit, int? offset, CancellationToken cancellationToken) =>
        FirstOrDefaultAsync(new Query<TEntity>(predicate, orderings, limit, offset), cancellationToken);

    public virtual ValueTask<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate,
        CancellationToken cancellationToken) => FirstOrDefaultAsync(new Query<TEntity>(predicate), cancellationToken);

    public virtual async ValueTask<TResult?> SingleOrDefaultAsync<TResult>(IQuery<TEntity, TResult> specQuery, CancellationToken cancellationToken)
    {
        var query = CreateQueryByQueryWithProjection(specQuery);
        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    public virtual async ValueTask<TEntity?> SingleOrDefaultAsync(IQuery<TEntity> specQuery, CancellationToken cancellationToken)
    {
        var query = CreateQueryByQuery(specQuery);
        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    public virtual ValueTask<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate,
        IQueryOrder<TEntity>[]? orderings, int? limit, int? offset, CancellationToken cancellationToken) =>
        SingleOrDefaultAsync(new Query<TEntity>(predicate, orderings, limit, offset), cancellationToken);

    public virtual ValueTask<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate,
        CancellationToken cancellationToken) => SingleOrDefaultAsync(new Query<TEntity>(predicate), cancellationToken);

    public virtual async ValueTask<TResult[]> ToArrayAsync<TResult>(IQuery<TEntity, TResult> specQuery, CancellationToken cancellationToken)
    {
        var query = CreateQueryByQueryWithProjection(specQuery);
        return await query.ToArrayAsync(cancellationToken);
    }

    public virtual async ValueTask<TEntity[]> ToArrayAsync(IQuery<TEntity> specQuery, CancellationToken cancellationToken)
    {
        var query = CreateQueryByQuery(specQuery);
        return await query.ToArrayAsync(cancellationToken);
    }

    public virtual ValueTask<TEntity[]> ToArrayAsync(Expression<Func<TEntity, bool>>? predicate,
        IQueryOrder<TEntity>[]? orderings, int? limit, int? offset, CancellationToken cancellationToken) =>
        ToArrayAsync(new Query<TEntity>(predicate, orderings, limit, offset), cancellationToken);

    public virtual async ValueTask<bool> AnyAsync(IQuery<TEntity> specQuery, CancellationToken cancellationToken)
    {
        var query = CreateQueryByQuery(specQuery);
        return await query.AnyAsync(cancellationToken);
    }

    public virtual ValueTask<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate,
        CancellationToken cancellationToken) => AnyAsync(new Query<TEntity>(predicate), cancellationToken);

    public virtual async ValueTask<bool> AnyAsync<TResult>(IQuery<TEntity, TResult> specQuery, CancellationToken cancellationToken)
    {
        var query = CreateQueryByQueryWithProjection(specQuery);
        return await query.AnyAsync(cancellationToken);
    }

    public virtual async ValueTask<TEntity[]> ToArrayAsync(CancellationToken cancellationToken)
    {
        var query = DbSet.AsQueryable();
        if (AsNoTracing)
        {
            query = query.AsNoTracking();
        }

        return await query.ToArrayAsync(cancellationToken);
    }

    public virtual async ValueTask<TEntity?> FirstOrDefaultAsync(CancellationToken cancellationToken)
    {
        var query = DbSet.AsQueryable();
        if (AsNoTracing)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async ValueTask<TEntity?> SingleOrDefaultAsync(CancellationToken cancellationToken)
    {
        var query = DbSet.AsQueryable();
        if (AsNoTracing)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    public virtual async ValueTask<bool> AnyAsync(CancellationToken cancellationToken)
    {
        var query = DbSet.AsQueryable();
        if (AsNoTracing)
        {
            query = query.AsNoTracking();
        }

        return await query.AnyAsync(cancellationToken);
    }

    public virtual async ValueTask<TEntity> SingleAsync(IQuery<TEntity> specQuery, CancellationToken cancellationToken)
    {
        var query = CreateQueryByQuery(specQuery);
        return await query.SingleAsync(cancellationToken);
    }

    public virtual ValueTask<TEntity> SingleAsync(Expression<Func<TEntity, bool>>? predicate,
        CancellationToken cancellationToken) => SingleAsync(new Query<TEntity>(predicate), cancellationToken);

    public virtual async ValueTask<TEntity> SingleAsync(CancellationToken cancellationToken)
    {
        var query = DbSet.AsQueryable();

        if (AsNoTracing)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleAsync(cancellationToken);
    }

    public virtual async ValueTask<TResult> SingleAsync<TResult>(IQuery<TEntity, TResult> specQuery, CancellationToken cancellationToken)
    {
        var query = CreateQueryByQueryWithProjection(specQuery);
        return await query.SingleAsync(cancellationToken);
    }

    private IQueryable<TEntity> CreateQueryByQuery(IQuery<TEntity> query)
    {
        var queryable = DbSet.AsQueryable();

        if (AsNoTracing)
        {
            queryable = queryable.AsNoTracking();
        }

        return query.ApplyQuery(queryable);
    }

    private IQueryable<TResult> CreateQueryByQueryWithProjection<TResult>(IQuery<TEntity, TResult> query)
    {
        if (query.Selector == null)
        {
            throw new ArgumentNullException(nameof(query.Selector));
        }

        var queryable = DbSet.AsQueryable();

        if (AsNoTracing)
        {
            queryable = queryable.AsNoTracking();
        }

        return query.ApplyQuery(queryable);
    }
}