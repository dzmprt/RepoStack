using Microsoft.EntityFrameworkCore;

namespace RepoStack.EntityFrameworkCore;

public class TransactionManager(DbContext dbContext) : ITransactionManager
{
    public bool IsTransactionStarted => dbContext.Database.CurrentTransaction != null;

    public async ValueTask BeginTransactionIfNotExistsAsync(CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction == null)
        {
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        }
    }

    public async ValueTask CommitTransactionIfExistsAsync(CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction != null)
        {
            await dbContext.Database.CommitTransactionAsync(cancellationToken);
        }
    }

    public async ValueTask RollbackTransactionIsExistsAsync()
    {
        if (dbContext.Database.CurrentTransaction != null)
        {
            await dbContext.Database.RollbackTransactionAsync();
        }
    }
}