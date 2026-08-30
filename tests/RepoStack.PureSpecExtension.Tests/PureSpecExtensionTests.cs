using PureSpec;
using RepoStack.PureSpecExtension;

namespace RepoStack.PureSpecExtension.Tests;

public sealed class PureSpecExtensionTests
{
    private sealed record Item(int Id, string Name, int Score);

    private sealed record ItemDto(string Name);

    private sealed class ItemProjection : ISpecificationProjection<Item, ItemDto>
    {
        public required System.Linq.Expressions.Expression<Func<Item, bool>> Predicate { get; init; }

        public required System.Linq.Expressions.Expression<Func<Item, ItemDto>> Selector { get; init; }

        public Func<Item, bool> CompilePredicate() => Predicate.Compile();

        public Func<Item, ItemDto> CompileSelector() => Selector.Compile();
    }

    private static IQueryable<Item> Items => new[]
    {
        new Item(1, "a", 3),
        new Item(2, "b", 1),
        new Item(3, "c", 2),
        new Item(4, "d", 4)
    }.AsQueryable();

    [Fact]
    public void ToQueryWithoutOrderingPreservesPredicate()
    {
        var specification = new Specification<Item>(item => item.Score > 1);

        var query = specification.ToQuery();

        Assert.Equal([1, 3, 4], query.ApplyQuery(Items).Select(item => item.Id));
        Assert.Null(query.Limit);
        Assert.Null(query.Offset);
        Assert.Empty(query.Orderings);
    }

    [Fact]
    public void ToQueryWithKeySelectorCreatesAscendingOrderedPagedQuery()
    {
        var specification = new Specification<Item>(item => item.Score > 0);

        var query = specification.ToQuery(item => item.Score, limit: 2, offset: 1);

        Assert.Equal([3, 1], query.ApplyQuery(Items).Select(item => item.Id));
        Assert.Equal(2, query.Limit);
        Assert.Equal(1, query.Offset);
    }

    [Fact]
    public void ToQueryWithDescendingKeySelectorPreservesDescendingOrdering()
    {
        var specification = new Specification<Item>(item => true);

        var query = specification.ToQuery(item => item.Score, descending: true);

        Assert.Equal([4, 1, 3, 2], query.ApplyQuery(Items).Select(item => item.Id));
    }

    [Fact]
    public void ToQueryWithCustomOrderingsPreservesAllOrderings()
    {
        var specification = new Specification<Item>(item => item.Score > 0);
        var orderings = new IQueryOrder<Item>[]
        {
            new QueryOrder<Item, int>(item => item.Score),
            new QueryOrder<Item, string>(item => item.Name, descending: true)
        };

        var query = specification.ToQuery(orderings, limit: 3);

        Assert.Equal([2, 3, 1], query.ApplyQuery(Items).Select(item => item.Id));
        Assert.Equal(orderings, query.Orderings);
        Assert.Equal(3, query.Limit);
    }

    [Fact]
    public void ProjectedToQueryPreservesPredicateSelectorAndPaging()
    {
        var specification = new ItemProjection
        {
            Predicate = item => item.Score > 1,
            Selector = item => new ItemDto(item.Name)
        };

        var query = specification.ToQuery(item => item.Id, limit: 2, offset: 1);

        Assert.Equal(["c", "d"], query.ApplyQuery(Items).Select(item => item.Name));
        Assert.Equal(2, query.Limit);
        Assert.Equal(1, query.Offset);
    }

    [Fact]
    public void ProjectedToQueryWithoutOrderingStillAppliesProjection()
    {
        var specification = new ItemProjection
        {
            Predicate = item => item.Score == 2,
            Selector = item => new ItemDto(item.Name)
        };

        var query = specification.ToQuery();

        Assert.Equal([new ItemDto("c")], query.ApplyQuery(Items));
        Assert.Empty(query.Orderings);
    }
}