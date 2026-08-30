using Books.Domain;
using MitMediator;
using Books.Application.DbQueries;
using RepoStack;

namespace Books.Application.UseCase.Books.Queries.GetBooksByFilter;

/// <summary>
/// Handler for <see cref="GetBooksByFilterQuery"/>.
/// </summary>
internal sealed class GetBooksByFilterQueryHandler(IProvider<Book> booksProvider) : IRequestHandler<GetBooksByFilterQuery, Book[]>
{
    /// <inheritdoc/>
    public async ValueTask<Book[]> HandleAsync(GetBooksByFilterQuery request, CancellationToken cancellationToken)
    {
        var freeText = request.FreeText?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(freeText))
        {
            freeText = null;
        }
        var query = new BookByFreeTextQuery(freeText, true, request.Limit, request.Offset);
        return await booksProvider.ToArrayAsync(query, cancellationToken);


    }
}