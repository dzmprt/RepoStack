using System.Linq.Expressions;
using RepoStack;

namespace RepoStack.Tests;

public sealed class QueryTests
{
    private sealed record Item(int Id, string Name, int Score);

    private static IQueryable<Item> Items => new[]
    {
        new Item(1, "a", 3),
        new Item(2, "b", 1),
        new Item(3, "c", 2),
        new Item(4, "d", 4)
    }.AsQueryable();

    [Fact]
    public void QueryAppliesPredicateOrderingPagingAndCopiesOrderings()
    {
        var orderings = new IQueryOrder<Item>[] { new QueryOrder<Item, int>(item => item.Score) };
        var query = new Query<Item>(item => item.Id > 1, orderings, limit: 2, offset: 1);
        orderings[0] = new QueryOrder<Item, int>(item => item.Id);

        var result = query.ApplyQuery(Items).ToArray();

        Assert.Equal([new Item(3, "c", 2), new Item(4, "d", 4)], result);
    }

    [Fact]
    public void QuerySupportsMultipleOrderingsAndNoPaging()
    {
        var query = new Query<Item>(item => item.Id > 0,
            [new QueryOrder<Item, int>(item => item.Score), new QueryOrder<Item, string>(item => item.Name, descending: true)]);

        Assert.Equal([2, 3, 1, 4], query.ApplyQuery(Items).Select(item => item.Id));
    }

    [Fact]
    public void QuerySupportsLimitAndOffsetIndependently()
    {
        var offsetOnly = new Query<Item>(orderings: [new QueryOrder<Item, int>(item => item.Id)], offset: 2);
        var limitOnly = new Query<Item>(orderings: [new QueryOrder<Item, int>(item => item.Id)], limit: 2);

        Assert.Equal([3, 4], offsetOnly.ApplyQuery(Items).Select(item => item.Id));
        Assert.Equal([1, 2], limitOnly.ApplyQuery(Items).Select(item => item.Id));
    }

    [Fact]
    public void ProjectedQueryAppliesAllOperationsAndSelector()
    {
        var query = new Query<Item, string>(item => item.Score > 0, item => item.Name,
            [new QueryOrder<Item, int>(item => item.Id, descending: true)], limit: 2, offset: 1);

        Assert.Equal(["c", "b"], query.ApplyQuery(Items));
    }

    [Fact]
    public void QueryConstructorsRejectInvalidArguments()
    {
        Expression<Func<Item, bool>> predicate = item => true;
        Assert.Equal(4, new Query<Item>(null!).ApplyQuery(Items).Count());
        Assert.Throws<ArgumentOutOfRangeException>(() => new Query<Item>(predicate, null, limit: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Query<Item>(predicate, null, offset: -1));
        Assert.Throws<ArgumentNullException>(() => new Query<Item>(predicate, new IQueryOrder<Item>[] { null! }));
        Assert.Equal(["a", "b", "c", "d"], new Query<Item, string>(null!, item => item.Name).ApplyQuery(Items));
        Assert.Throws<ArgumentNullException>(() => new Query<Item, string>(predicate, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Query<Item, string>(predicate, item => item.Name, limit: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Query<Item, string>(predicate, item => item.Name, offset: -1));
        Assert.Throws<ArgumentNullException>(() => new Query<Item, string>(predicate, item => item.Name, new IQueryOrder<Item>[] { null! }));
    }

    [Fact]
    public void ProjectedQuerySupportsDefaultsWithoutOrderingOrPaging()
    {
        var query = new Query<Item, string>(item => item.Id > 0, item => item.Name);
        var orderedQuery = new Query<Item, string>(item => item.Id > 0, item => item.Name,
            [new QueryOrder<Item, int>(item => item.Score), new QueryOrder<Item, int>(item => item.Id)]);

        Assert.Equal(["a", "b", "c", "d"], query.ApplyQuery(Items));
        Assert.Equal(["b", "c", "a", "d"], orderedQuery.ApplyQuery(Items));
    }

    [Fact]
    public void ApplyQueryRejectsNullSource()
    {
        var query = new Query<Item>(item => true);
        var projected = new Query<Item, string>(item => true, item => item.Name);

        Assert.Throws<ArgumentNullException>(() => query.ApplyQuery(null!));
        Assert.Throws<ArgumentNullException>(() => projected.ApplyQuery(null!));
    }

    [Fact]
    public void QueryOrderSupportsAscendingAndDescendingThenBy()
    {
        var ascending = new QueryOrder<Item, int>(item => item.Score);
        var descending = new QueryOrder<Item, int>(item => item.Score, descending: true);
        var ordered = ascending.Apply(Items);

        Assert.Equal([2, 3, 1, 4], ordered.Select(item => item.Id));
        Assert.Equal([4, 1, 3, 2], descending.Apply(Items).Select(item => item.Id));
        Assert.Equal([2, 3, 1, 4], ascending.ApplyThen(ordered).Select(item => item.Id));
        Assert.Throws<ArgumentNullException>(() => new QueryOrder<Item, int>(null!));
        Assert.Throws<ArgumentNullException>(() => ascending.Apply(null!));
        Assert.Throws<ArgumentNullException>(() => ascending.ApplyThen(null!));
    }

    [Fact]
    public void QueryKeepsAnIndependentReadOnlyOrderingSnapshot()
    {
        var orderings = new List<IQueryOrder<Item>>
        {
            new QueryOrder<Item, int>(item => item.Score)
        };
        var query = new Query<Item>(orderings: orderings);

        orderings.Clear();

        Assert.Single(query.Orderings);
        var readOnlyOrderings = Assert.IsAssignableFrom<IList<IQueryOrder<Item>>>(query.Orderings);
        Assert.Throws<NotSupportedException>(() => readOnlyOrderings.Add(null!));
    }
}
