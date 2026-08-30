namespace RepoStack;

/// <summary>
/// Controls the current database transaction.
/// </summary>
public interface ITransactionManager
{
    /// <summary>
    /// Gets a value indicating whether a transaction is active.
    /// </summary>
    bool IsTransactionStarted { get; }

    /// <summary>
    /// Starts a transaction when one is not already active.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask BeginTransactionIfNotExistsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Commits the active transaction.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask CommitTransactionIfExistsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Rolls back the active transaction.
    /// </summary>
    ValueTask RollbackTransactionIsExistsAsync();
}