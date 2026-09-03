using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RepoStack;

namespace RepoStack.EntityFrameworkCore.Tests;

public sealed class EfCoreTests
{
    private sealed class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Score { get; set; }
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<Item> Items => Set<Item>();
    }

    private sealed class NullSelectorQuery : IQuery<Item, string>
    {
        public System.Linq.Expressions.Expression<Func<Item, string>> Selector => null!;
        public System.Linq.Expressions.Expression<Func<Item, bool>>? Predicate => null;
        public int? Limit => null;
        public int? Offset => null;
        public IReadOnlyList<IQueryOrder<Item>> Orderings => [];
        public IQueryable<string> ApplyQuery(IQueryable<Item> source) => throw new NotSupportedException();
    }

    private static (SqliteConnection Connection, TestDbContext Context) CreateContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new TestDbContext(options);
        context.Database.EnsureCreated();
        context.Items.AddRange(
            new Item { Name = "one", Score = 1 },
            new Item { Name = "two", Score = 2 },
            new Item { Name = "three", Score = 3 });
        context.SaveChanges();
        return (connection, context);
    }

    [Fact]
    public async Task ProviderSupportsUnspecifiedQueriesAndNoTracking()
    {
        var (connection, context) = CreateContext();
        await using (connection)
        await using (context)
        {
            context.ChangeTracker.Clear();
            var provider = new BaseProvider<Item>(context);

            Assert.Equal(3, (await provider.ToArrayAsync(CancellationToken.None)).Length);
            Assert.NotNull(await provider.FirstOrDefaultAsync(CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                provider.SingleOrDefaultAsync(CancellationToken.None).AsTask());
            Assert.True(await provider.AnyAsync(CancellationToken.None));
            Assert.Empty(context.ChangeTracker.Entries());
        }
    }

    [Fact]
    public async Task ProviderSupportsFilteredProjectedAndCountOperations()
    {
        var (connection, context) = CreateContext();
        await using (connection)
        await using (context)
        {
            var provider = new BaseProvider<Item>(context);
            var query = new Query<Item>(item => item.Score > 1,
                new[] { new QueryOrder<Item, int>(item => item.Score, descending: true) }, limit: 2);
            var projectedQuery = new Query<Item, string>(item => item.Score > 1, item => item.Name,
                new[] { new QueryOrder<Item, int>(item => item.Score, descending: true) });

            Assert.Equal(2, await provider.CountAsync(query, CancellationToken.None));
            Assert.Equal(2, await provider.CountAsync(projectedQuery, CancellationToken.None));
            Assert.Equal(new[] { "three", "two" }, await provider.ToArrayAsync(projectedQuery, CancellationToken.None));
            Assert.True(await provider.AnyAsync(projectedQuery, CancellationToken.None));
            Assert.Equal("three", await provider.FirstOrDefaultAsync(projectedQuery, CancellationToken.None));
            Assert.Equal("two", await provider.SingleOrDefaultAsync(
                new Query<Item, string>(item => item.Score == 2, item => item.Name), CancellationToken.None));
            Assert.Null(await provider.FirstOrDefaultAsync(
                new Query<Item>(item => item.Score == 999), CancellationToken.None));
        }
    }

    [Fact]
    public async Task ProviderSupportsExpressionBasedOverloads()
    {
        var (connection, context) = CreateContext();
        await using (connection)
        await using (context)
        {
            var provider = new BaseProvider<Item>(context);
            var orderings = new IQueryOrder<Item>[]
            {
                new QueryOrder<Item, int>(item => item.Score, descending: true)
            };

            Assert.Equal("two", (await provider.ToArrayAsync(
                item => item.Score > 0, orderings, limit: 1, offset: 1, CancellationToken.None))[0].Name);
            Assert.Equal("three", (await provider.FirstOrDefaultAsync(
                item => item.Score > 2, [], null, null, CancellationToken.None))!.Name);
            Assert.Equal("two", (await provider.SingleOrDefaultAsync(
                item => item.Score == 2, [], null, null, CancellationToken.None))!.Name);
            Assert.Equal("two", (await provider.SingleAsync(item => item.Score == 2, CancellationToken.None)).Name);
            Assert.Equal(2, await provider.CountAsync(item => item.Score > 1, CancellationToken.None));
            Assert.True(await provider.AnyAsync(item => item.Name == "three", CancellationToken.None));
        }
    }

    [Fact]
    public async Task ProviderExecutesSingleOperationsAndCanTrackEntities()
    {
        var (connection, context) = CreateContext();
        await using (connection)
        await using (context)
        {
            var provider = new BaseProvider<Item>(context, useAsNoTracing: false);
            var query = new Query<Item>(item => item.Score == 2);
            var projectedQuery = new Query<Item, string>(item => item.Score == 2, item => item.Name);

            Assert.Equal(3, (await provider.ToArrayAsync(CancellationToken.None)).Length);
            Assert.NotNull(await provider.FirstOrDefaultAsync(CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.SingleOrDefaultAsync(CancellationToken.None).AsTask());
            Assert.True(await provider.AnyAsync(CancellationToken.None));
            Assert.Equal("two", (await provider.SingleAsync(query, CancellationToken.None)).Name);
            Assert.Equal("two", await provider.SingleAsync(projectedQuery, CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.SingleAsync(CancellationToken.None).AsTask());

            context.Items.RemoveRange(context.Items);
            await context.SaveChangesAsync();
            context.Items.Add(new Item { Name = "only", Score = 5 });
            await context.SaveChangesAsync();

            Assert.Equal("only", (await provider.SingleOrDefaultAsync(CancellationToken.None))!.Name);
            Assert.Equal("only", (await provider.SingleAsync(CancellationToken.None)).Name);
            var noTrackingProvider = new BaseProvider<Item>(context);
            Assert.Equal("only", (await noTrackingProvider.SingleAsync(CancellationToken.None)).Name);
            Assert.NotEmpty(context.ChangeTracker.Entries());
        }

    }

    [Fact]
    public async Task RepositorySupportsBatchAddUpdateAndDelete()
    {
        var (connection, context) = CreateContext();
        await using (connection)
        await using (context)
        {
            var repository = new BaseRepository<Item>(context, new TransactionManager(context));
            var added = new[]
            {
                new Item { Name = "four", Score = 4 },
                new Item { Name = "five", Score = 5 }
            };

            await repository.AddManyAsync(added, CancellationToken.None);
            Assert.Equal(5, await repository.CountAsync(CancellationToken.None));

            added[0].Name = "updated-four";
            added[1].Name = "updated-five";
            await repository.UpdateManyAsync(added, CancellationToken.None);
            Assert.Equal(new[] { "updated-four", "updated-five" }, await repository.ToArrayAsync(
                new Query<Item, string>(item => item.Score > 3, item => item.Name,
                    new[] { new QueryOrder<Item, int>(item => item.Score) }), CancellationToken.None));

            var deleted = await repository.DeleteManyAsync(
                new Query<Item>(item => item.Score > 3), CancellationToken.None);
            Assert.Equal(2, deleted);
            Assert.Equal(3, await repository.CountAsync(CancellationToken.None));
        }
    }

    [Fact]
    public async Task ProviderRejectsProjectedQueryWithoutSelector()
    {
        var (connection, context) = CreateContext();
        await using (connection)
        await using (context)
        {
            var provider = new BaseProvider<Item>(context);

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                provider.FirstOrDefaultAsync(new NullSelectorQuery(), CancellationToken.None).AsTask());
        }
    }

    [Fact]
    public async Task RepositoryStartsTransactionWhenConfigured()
    {
        var previousSetting = DependencyInjection.StartTransactionsBeforeAnyModification;
        new ServiceCollection().AddRepoStackEntityFrameworkCore(startTransactionsBeforeAnyModification: true);
        try
        {
            var (connection, context) = CreateContext();
            await using (connection)
            await using (context)
            {
                var manager = new TransactionManager(context);
                var repository = new BaseRepository<Item>(context, manager);

                await repository.AddAsync(new Item { Name = "transactional", Score = 6 }, CancellationToken.None);

                Assert.True(manager.IsTransactionStarted);
                await manager.RollbackTransactionIsExistsAsync();
                Assert.Null(await repository.FirstOrDefaultAsync(
                    new Query<Item>(item => item.Name == "transactional"), CancellationToken.None));
            }
        }
        finally
        {
            new ServiceCollection().AddRepoStackEntityFrameworkCore(previousSetting);
        }
    }

    [Fact]
    public async Task RepositoryAddsUpdatesAndDeletesWithinTransactions()
    {
        var previousSetting = DependencyInjection.StartTransactionsBeforeAnyModification;
        new ServiceCollection().AddRepoStackEntityFrameworkCore(startTransactionsBeforeAnyModification: true);
        try
        {
        var (connection, context) = CreateContext();
        await using (connection)
        await using (context)
        {
            var manager = new TransactionManager(context);
            var repository = new BaseRepository<Item>(context, manager);
            var item = new Item { Name = "new", Score = 4 };

            await repository.AddAsync(item, CancellationToken.None);
            Assert.True(manager.IsTransactionStarted);
            await manager.CommitTransactionIfExistsAsync(CancellationToken.None);
            Assert.NotEqual(0, item.Id);

            item.Name = "updated";
            await repository.UpdateAsync(item, CancellationToken.None);
            await manager.CommitTransactionIfExistsAsync(CancellationToken.None);
            Assert.Equal("updated", (await repository.FirstOrDefaultAsync(new Query<Item>(x => x.Id == item.Id), CancellationToken.None))!.Name);

            await repository.DeleteAsync(new Query<Item>(x => x.Id == item.Id), CancellationToken.None);
            await manager.CommitTransactionIfExistsAsync(CancellationToken.None);
            Assert.Null(await repository.FirstOrDefaultAsync(new Query<Item>(x => x.Id == item.Id), CancellationToken.None));
        }
        }
        finally
        {
            new ServiceCollection().AddRepoStackEntityFrameworkCore(previousSetting);
        }
    }

    [Fact]
    public async Task RepositoryDeleteThrowsWhenEntityIsMissing()
    {
        var (connection, context) = CreateContext();
        await using (connection)
        await using (context)
        {
            var repository = new BaseRepository<Item>(context, new TransactionManager(context));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repository.DeleteAsync(new Query<Item>(item => item.Id == 999), CancellationToken.None).AsTask());
        }
    }

    [Fact]
    public async Task TransactionManagerHandlesMissingAndActiveTransactions()
    {
        var (connection, context) = CreateContext();
        await using (connection)
        await using (context)
        {
            var manager = new TransactionManager(context);
            await manager.CommitTransactionIfExistsAsync(CancellationToken.None);
            await manager.RollbackTransactionIsExistsAsync();
            Assert.False(manager.IsTransactionStarted);

            await manager.BeginTransactionIfNotExistsAsync(CancellationToken.None);
            await manager.BeginTransactionIfNotExistsAsync(CancellationToken.None);
            Assert.True(manager.IsTransactionStarted);
            await manager.RollbackTransactionIsExistsAsync();
            Assert.False(manager.IsTransactionStarted);
        }
    }

    [Fact]
    public void DependencyInjectionRegistersProviderRepositoryAndTransactionManager()
    {
        var services = new ServiceCollection();
        services.AddRepoStackEntityFrameworkCore();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IProvider<>));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRepository<>));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ITransactionManager));
    }

    [Fact]
    public void DependencyInjectionConfiguresTransactionSettingAndLifetimes()
    {
        var previousSetting = DependencyInjection.StartTransactionsBeforeAnyModification;
        try
        {
            var services = new ServiceCollection();
            var returnedServices = services.AddRepoStackEntityFrameworkCore(true);

            Assert.Same(services, returnedServices);
            Assert.True(DependencyInjection.StartTransactionsBeforeAnyModification);
            Assert.Equal(ServiceLifetime.Transient, services.Single(x => x.ServiceType == typeof(IProvider<>)).Lifetime);
            Assert.Equal(ServiceLifetime.Transient, services.Single(x => x.ServiceType == typeof(IRepository<>)).Lifetime);
            Assert.Equal(ServiceLifetime.Scoped, services.Single(x => x.ServiceType == typeof(ITransactionManager)).Lifetime);
        }
        finally
        {
            new ServiceCollection().AddRepoStackEntityFrameworkCore(previousSetting);
        }
    }
}
