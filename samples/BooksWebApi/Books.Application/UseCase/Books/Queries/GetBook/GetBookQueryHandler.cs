using Books.Application.Exceptions;
using Books.Domain;
using MitMediator;
using Books.Application.DbQueries;
using RepoStack;

namespace Books.Application.UseCase.Books.Queries.GetBook;

/// <summary>
/// Handler for <see cref="GetBookQuery"/>.
/// </summary>
internal sealed class GetBookQueryHandler(IProvider<Book> booksProvider) : IRequestHandler<GetBookQuery, Book>
{
    /// <inheritdoc/>
    public async ValueTask<Book> HandleAsync(GetBookQuery query, CancellationToken cancellationToken)
    {
        var dbQuery = new BookByIdQuery(query.BookId, true);
        var book = await booksProvider.SingleOrDefaultAsync(dbQuery, cancellationToken);
        if (book is null)
        {
            throw new NotFoundException();
        }

        return book;
    }
}