# RepoStack

[![Build and Test](https://github.com/dzmprt/RepoStack/actions/workflows/dotnet.yml/badge.svg)](https://github.com/dzmprt/RepoStack/actions/workflows/dotnet.yml)
![NuGet](https://img.shields.io/nuget/v/RepoStack)
![.NET 10](https://img.shields.io/badge/Version-.NET%2010-informational?style=flat&logo=dotnet)
![License](https://img.shields.io/github/license/dzmprt/RepoStack)

Reusable repository and query abstractions for .NET applications.

RepoStack keeps application code independent from a concrete data-access implementation. The core package contains provider, repository, query, ordering, and transaction contracts. Database-specific integrations live in separate packages; Entity Framework Core is currently provided by `RepoStack.EntityFrameworkCore`.

## Contents

- [Features](#features)
- [Installation](#installation)
- [Queries and projections](#queries-and-projections)
- [Write operations](#write-operations)
- [Transactions](#transactions)
- [Quick start with Entity Framework Core](#quick-start-with-entity-framework-core)
- [PureSpec integration](#purespec-integration)
- [License](#license)

## Features

- Generic read providers through `IProvider<TEntity>`
- Generic repositories through `IRepository<TEntity>`
- Filtered, ordered, and paged queries with `Query<TEntity>`
- Server-side projections with `Query<TEntity, TResult>`
- `ToArrayAsync`, `FirstOrDefaultAsync`, `SingleOrDefaultAsync`, `SingleAsync`, `CountAsync`, and `AnyAsync`
- Single-item and batch add, update, and delete operations
- Optional automatic transaction start for repository mutations
- Cancellation support on asynchronous database operations
- No-tracking reads by default in the EF Core provider


## Installation

Install the core abstractions package:

```bash
dotnet add package RepoStack --version 0.0.1-alpha
```

Install the Entity Framework Core integration when using EF Core:

```bash
dotnet add package RepoStack.EntityFrameworkCore --version 0.0.1-alpha
```

Install the PureSpec integration when the application uses [PureSpec](https://github.com/dzmprt/PureSpec) specifications:

```bash
dotnet add package RepoStack.PureSpecExtension --version 0.0.1-alpha
```

## Queries and projections

`Query<TEntity>` describes an entity query. Its optional predicate, orderings, offset, and limit are applied in that order. `Query<TEntity, TResult>` adds a LINQ expression for server-side projection:

```csharp
var query = new Query<Book, BookListItem>(
	predicate: book => book.Genre == Genre.Fiction,
	selector: book => new BookListItem(book.Id, book.Title),
	orderings: [
		new QueryOrder<Book, string>(book => book.Title),
		new QueryOrder<Book, int>(book => book.Id)
	],
	offset: 20,
	limit: 20);

BookListItem[] page = await books.ToArrayAsync(query, cancellationToken);
```

Use `descending: true` for descending order. Multiple `QueryOrder` instances become a primary ordering followed by `ThenBy` / `ThenByDescending` orderings.

## Write operations

Use `IRepository<TEntity>` when the service needs mutations as well as reads:

```csharp
public sealed class BookService(IRepository<Book> books)
{
	public async ValueTask AddAsync(Book book, CancellationToken cancellationToken)
	{
		await books.AddAsync(book, cancellationToken);
	}

	public async ValueTask DeleteArchivedAsync(CancellationToken cancellationToken)
	{
		await books.DeleteManyAsync(
			new Query<Book>(book => book.IsArchived),
			cancellationToken);
	}
}
```

`AddAsync`, `AddManyAsync`, `UpdateAsync`, `UpdateManyAsync`, and `DeleteAsync` save changes through the current `DbContext`. `DeleteManyAsync` uses EF Core's set-based `ExecuteDeleteAsync` and returns the number of deleted rows.

## Transactions

By default, repository mutations use the current EF Core transaction when one exists. To automatically start a transaction before a mutation, enable the option during registration:

```csharp
services.AddRepoStackEntityFrameworkCore(
	startTransactionsBeforeAnyModification: true);
```

When this option is enabled, commit or roll back through the injected `ITransactionManager`:

```csharp
public sealed class ImportService(
	IRepository<Book> books,
	ITransactionManager transactions)
{
	public async ValueTask ImportAsync(
		Book[] importedBooks,
		CancellationToken cancellationToken)
	{
		await books.AddManyAsync(importedBooks, cancellationToken);
		await transactions.CommitTransactionIfExistsAsync(cancellationToken);
	}
}
```

The manager also exposes `BeginTransactionIfNotExistsAsync` and `RollbackTransactionIsExistsAsync` for explicit transaction control. The transaction is tied to the registered `DbContext` scope.

## Quick start with Entity Framework Core

Register the application's `DbContext` and RepoStack in the service collection:

```csharp
using Microsoft.EntityFrameworkCore;
using RepoStack.EntityFrameworkCore;

services
	.AddDbContext<DbContext, BooksDbContext>(options =>
		options.UseSqlServer(connectionString))
	.AddRepoStackEntityFrameworkCore();
```

`AddRepoStackEntityFrameworkCore()` registers:

| Abstraction | Default implementation | Lifetime |
| --- | --- | --- |
| `IProvider<TEntity>` | `BaseProvider<TEntity>` | Transient |
| `IRepository<TEntity>` | `BaseRepository<TEntity>` | Transient |
| `ITransactionManager` | `TransactionManager` | Scoped |

Inject the abstraction you need into an application service or handler:

```csharp
using RepoStack;

public sealed class BookService(IProvider<Book> books)
{
	public ValueTask<Book[]> GetAvailableBooksAsync(CancellationToken cancellationToken) =>
		books.ToArrayAsync(
			new Query<Book>(
				predicate: book => book.IsAvailable,
				orderings: [new QueryOrder<Book, string>(book => book.Title)],
				limit: 20),
			cancellationToken);
}
```

## PureSpec integration

`RepoStack.PureSpecExtension` bridges PureSpec specifications and RepoStack queries. It adds `ToQuery(...)` extension methods for both `ISpecification<TEntity>` and `IProjectedSpecification<TEntity, TResult>`.

The extensions preserve the specification predicate and, for projected specifications, the selector. They can also add:

- no ordering or a custom `IQueryOrder<TEntity>` sequence;
- ascending or descending ordering from a key selector;
- optional `limit` and `offset` paging.

```csharp
using PureSpec;
using RepoStack;
using RepoStack.PureSpecExtension;

public sealed class BookService(IProvider<Book> books)
{
	public ValueTask<Book[]> FindAsync(
		ISpecification<Book> specification,
		CancellationToken cancellationToken) =>
		books.ToArrayAsync(
			specification.ToQuery(
				ordering: book => book.Title,
				limit: 20,
				offset: 0),
			cancellationToken);
}
```

For a projected PureSpec specification, the same API returns `IQuery<TEntity, TResult>` and keeps the projection server-side:

```csharp
IProjectedSpecification<Book, BookListItem> specification = GetBookListSpecification();

BookListItem[] result = await books.ToArrayAsync(
	specification.ToQuery(book => book.Title, descending: false, limit: 20),
	cancellationToken);
```

Use the overload accepting `IEnumerable<IQueryOrder<TEntity>>` when multiple ordering criteria are needed. `RepoStack.PureSpecExtension` does not provide a database provider; it only adapts PureSpec specifications for use with RepoStack providers and repositories.

## License

[MIT License](LICENSE).