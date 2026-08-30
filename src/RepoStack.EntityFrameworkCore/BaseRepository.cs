using Microsoft.EntityFrameworkCore;

namespace RepoStack.EntityFrameworkCore;

public class BaseRepository<TEntity>(DbContext dbContext, ITransactionManager transactionManager) :
    BaseProvider<TEntity>(dbContext, false),
    IRepository<TEntity> where TEntity : class
{
    public virtual async ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        DbSet.Add(entity);
        await BeginTransactionIfNotStarted(cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public virtual async ValueTask AddManyAsync(TEntity[] entities, CancellationToken cancellationToken)
    {
        DbSet.AddRange(entities);
        await BeginTransactionIfNotStarted(cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public virtual async ValueTask DeleteAsync(IQuery<TEntity> specification, CancellationToken cancellationToken)
    {
        var entity = await SingleAsync(specification, cancellationToken);
        DbSet.Remove(entity);
        await BeginTransactionIfNotStarted(cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public virtual async ValueTask<int> DeleteManyAsync(IQuery<TEntity> specification, CancellationToken cancellationToken)
    {
        await BeginTransactionIfNotStarted(cancellationToken);
        return await DbSet.Where(specification.Predicate!).ExecuteDeleteAsync(cancellationToken);
    }

    public virtual async ValueTask UpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        DbSet.Update(entity);
        await BeginTransactionIfNotStarted(cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public virtual async ValueTask UpdateManyAsync(TEntity[] entities, CancellationToken cancellationToken)
    {
        DbSet.UpdateRange(entities);
        await BeginTransactionIfNotStarted(cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    private async ValueTask BeginTransactionIfNotStarted(CancellationToken cancellationToken)
    {
        if (DependencyInjection.StartTransactionsBeforeAnyModification && !transactionManager.IsTransactionStarted)
        {
            await transactionManager.BeginTransactionIfNotExistsAsync(cancellationToken);
        }
    }
}