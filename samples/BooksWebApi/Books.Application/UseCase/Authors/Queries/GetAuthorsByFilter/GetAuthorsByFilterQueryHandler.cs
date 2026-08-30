using Books.Domain;
using MitMediator;
using Books.Application.DbQueries;
using RepoStack;

namespace Books.Application.UseCase.Authors.Queries.GetAuthorsByFilter;

/// <summary>
/// Handler for <see cref="GetAuthorsByFilterQuery"/>.
/// </summary>
internal sealed class GetAuthorsByFilterQueryHandler(IProvider<Author> authorProvider) : IRequestHandler<GetAuthorsByFilterQuery, Author[]>
{
    /// <inheritdoc/>
    public async ValueTask<Author[]> HandleAsync(GetAuthorsByFilterQuery request, CancellationToken cancellationToken)
    {
        var freeText = request.FreeText?.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(freeText))
        {
            var byFreeTextQuery = new AuthorByFreeTextQuery(freeText, false, request.Limit, request.Offset);
            return await authorProvider.ToArrayAsync(byFreeTextQuery, cancellationToken);
        }
        else
        {
            var emptyQuery = new Query<Author>(null, null, request.Limit, request.Offset);
            return await authorProvider.ToArrayAsync(emptyQuery, cancellationToken);
        }

    }
}