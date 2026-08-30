using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RepoStack.EntityFrameworkCore;
using RepoStack;

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var services = new ServiceCollection();
services.AddDbContext<SampleDbContext>(options => options.UseSqlite(connection));
services.AddScoped<DbContext>(serviceProvider => serviceProvider.GetRequiredService<SampleDbContext>());
services.AddRepoStackEntityFrameworkCore(startTransactionsBeforeAnyModification: true);

await using var serviceProvider = services.BuildServiceProvider();
await using var scope = serviceProvider.CreateAsyncScope();
var context = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
await context.Database.EnsureCreatedAsync();

var repository = scope.ServiceProvider.GetRequiredService<IRepository<Product>>();
var provider = scope.ServiceProvider.GetRequiredService<IProvider<Product>>();
var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
var cancellationToken = CancellationToken.None;

var products = new[]
{
	new Product { Name = "Keyboard", Category = "Hardware", Price = 80, Stock = 12 },
	new Product { Name = "Monitor", Category = "Hardware", Price = 240, Stock = 5 },
	new Product { Name = "Notebook", Category = "Stationery", Price = 8, Stock = 40 }
};

await repository.AddManyAsync(products, cancellationToken);
await transactionManager.CommitTransactionIfExistsAsync(cancellationToken);

var extraProduct = new Product { Name = "Pen", Category = "Stationery", Price = 3, Stock = 100 };
await repository.AddAsync(extraProduct, cancellationToken);
await transactionManager.CommitTransactionIfExistsAsync(cancellationToken);

var orderedHardware = new Query<Product>(
	product => product.Category == "Hardware",
	[
		new QueryOrder<Product, int>(product => product.Price, descending: true),
		new QueryOrder<Product, string>(product => product.Name)
	],
	limit: 1,
	offset: 0);
var hardwareNames = new Query<Product, string>(
	product => product.Category == "Hardware",
	product => product.Name,
	new[] { new QueryOrder<Product, int>(product => product.Price, descending: true) });
var monitorQuery = new Query<Product>(product => product.Name == "Monitor");
var monitorNameQuery = new Query<Product, string>(product => product.Name == "Monitor", product => product.Name);

Console.WriteLine($"All: {JsonSerializer.Serialize(await provider.ToArrayAsync(cancellationToken))}");
Console.WriteLine($"Filtered and paged: {JsonSerializer.Serialize(await provider.ToArrayAsync(orderedHardware, cancellationToken))}");
Console.WriteLine($"Projected: {JsonSerializer.Serialize(await provider.ToArrayAsync(hardwareNames, cancellationToken))}");
Console.WriteLine($"First: {(await provider.FirstOrDefaultAsync(cancellationToken))?.Name}");
Console.WriteLine($"First filtered: {(await provider.FirstOrDefaultAsync(monitorQuery, cancellationToken))?.Name}");
Console.WriteLine($"First projected: {await provider.FirstOrDefaultAsync(monitorNameQuery, cancellationToken)}");
Console.WriteLine($"Single filtered: {(await provider.SingleOrDefaultAsync(monitorQuery, cancellationToken))?.Name}");
Console.WriteLine($"Single projected: {await provider.SingleOrDefaultAsync(monitorNameQuery, cancellationToken)}");
Console.WriteLine($"Single required: {(await provider.SingleAsync(monitorQuery, cancellationToken)).Name}");
Console.WriteLine($"Single projected required: {await provider.SingleAsync(monitorNameQuery, cancellationToken)}");
Console.WriteLine($"Count all: {await provider.CountAsync(cancellationToken)}");
Console.WriteLine($"Count filtered: {await provider.CountAsync(orderedHardware, cancellationToken)}");
Console.WriteLine($"Count projected: {await provider.CountAsync(hardwareNames, cancellationToken)}");
Console.WriteLine($"Any all: {await provider.AnyAsync(cancellationToken)}");
Console.WriteLine($"Any filtered: {await provider.AnyAsync(orderedHardware, cancellationToken)}");
Console.WriteLine($"Any projected: {await provider.AnyAsync(hardwareNames, cancellationToken)}");

products[0].Stock = 10;
await repository.UpdateAsync(products[0], cancellationToken);
await transactionManager.CommitTransactionIfExistsAsync(cancellationToken);

products[1].Stock = 3;
products[2].Price = 9;
await repository.UpdateManyAsync(new[] { products[1], products[2] }, cancellationToken);
await transactionManager.CommitTransactionIfExistsAsync(cancellationToken);

var removed = await repository.DeleteManyAsync(
	new Query<Product>(product => product.Category == "Stationery"), cancellationToken);
await transactionManager.CommitTransactionIfExistsAsync(cancellationToken);
Console.WriteLine($"Deleted in batch: {removed}");

await repository.DeleteAsync(new Query<Product>(product => product.Name == "Monitor"), cancellationToken);
await transactionManager.CommitTransactionIfExistsAsync(cancellationToken);
var remaining = await provider.SingleOrDefaultAsync(cancellationToken);
Console.WriteLine($"Single remaining: {remaining?.Name}");
Console.WriteLine($"Single remaining required: {(await provider.SingleAsync(cancellationToken)).Name}");

var rollbackProduct = new Product { Name = "Rolled back", Category = "Demo", Price = 1, Stock = 1 };
await transactionManager.BeginTransactionIfNotExistsAsync(cancellationToken);
await repository.AddAsync(rollbackProduct, cancellationToken);
await transactionManager.RollbackTransactionIsExistsAsync();

Console.WriteLine($"Transaction active after rollback: {transactionManager.IsTransactionStarted}");
Console.WriteLine($"Rolled back item exists: {await provider.AnyAsync(new Query<Product>(product => product.Name == "Rolled back"), cancellationToken)}");
Console.WriteLine($"Remaining: {await provider.CountAsync(cancellationToken)}");

public sealed class Product
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Category { get; set; } = string.Empty;
	public int Price { get; set; }
	public int Stock { get; set; }
}

public sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
{
	public DbSet<Product> Products => Set<Product>();
}