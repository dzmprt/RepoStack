using Microsoft.Extensions.DependencyInjection;

namespace RepoStack.EntityFrameworkCore;

/// <summary>
/// Dependency injection.
/// </summary>
public static class DependencyInjection
{
    public static bool StartTransactionsBeforeAnyModification { get; private set; }

    /// <summary>
    /// Add RepoStack providers, repositories and transaction manager for EntityFrameworkCore:
    /// <see cref="IProvider{}"/> -> <see cref="BaseProvider{}"/>;
    /// <see cref="IRepository{}"/> -> <see cref="BaseRepository{}"/>;
    /// <see cref="ITransactionManager"/> -> <see cref="TransactionManager"/>;
    /// </summary>
    /// <param name="services"><see cref="IServiceProvider"/>.</param>
    /// <param name="startTransactionsBeforeAnyModification">Run transactions by default before any database
    /// modification operation. Start a transaction by default before any database modification operation. The
    /// transaction must be committed via ITransactionManager.</param>
    /// <returns><see cref="IServiceProvider"/></returns>
    public static IServiceCollection AddRepoStackEntityFrameworkCore(this IServiceCollection services, bool startTransactionsBeforeAnyModification = false)
    {
        StartTransactionsBeforeAnyModification = startTransactionsBeforeAnyModification;

        return services
            .AddTransient(typeof(IProvider<>), typeof(BaseProvider<>))
            .AddTransient(typeof(IRepository<>), typeof(BaseRepository<>))
            .AddScoped<ITransactionManager, TransactionManager>();
    }
}