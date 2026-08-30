using Books.Application.Exceptions;
using Books.Domain;
using MitMediator;
using Books.Application.DbQueries;
using RepoStack;

namespace Books.Application.UseCase.Books.Commands.UnpublishBook;

/// <summary>
/// Handler for <see cref="DeleteBookCommand"/>.
/// </summary>
internal sealed class UnpublishBookCommandHandler(IRepository<Book> booksRepository) : IRequestHandler<UnpublishBookCommand>
{
    /// <inheritdoc/>
    public async ValueTask<Unit> HandleAsync(UnpublishBookCommand command, CancellationToken cancellationToken)
    {
        var bookQuery = new BookByIdQuery(command.BookId, null);

        var book = await booksRepository.FirstOrDefaultAsync(bookQuery, cancellationToken);
        if (book is null)
        {
            throw new NotFoundException();
        }

        if (!book.IsPublished)
        {
            throw new BookIsNotAvailableException(command.BookId);
        }

        book.Unpublish();

        await booksRepository.UpdateAsync(book, cancellationToken);
        return Unit.Value;
    }
}