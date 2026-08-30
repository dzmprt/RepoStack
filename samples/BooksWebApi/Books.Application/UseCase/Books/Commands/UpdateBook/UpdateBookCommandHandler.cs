using Books.Application.Exceptions;
using Books.Application.UseCase.Books.Commands.CreateBook;
using Books.Domain;
using MitMediator;
using Books.Application.DbQueries;
using RepoStack;

namespace Books.Application.UseCase.Books.Commands.UpdateBook;

/// <summary>
/// Handler for <see cref="CreateBookCommand"/>
/// </summary>
public class UpdateBookCommandHandler(
    IRepository<Book> booksRepository,
    IRepository<Author> authorsRepository,
    IRepository<Genre> genresRepository) :
    IRequestHandler<UpdateBookCommand, Book>
{
    /// <inheritdoc/>
    public async ValueTask<Book> HandleAsync(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        var bookQuery = new BookByIdQuery(request.BookId, null);

        var book = await booksRepository.FirstOrDefaultAsync(bookQuery, cancellationToken);
        if (book is null)
        {
            throw new NotFoundException();
        }
        var authorQuery = new AuthorByIdQuery(request.AuthorId);

        var author = await authorsRepository.FirstOrDefaultAsync(authorQuery, cancellationToken);
        if (author is null)
        {
            throw new BadOperationException("Author not found");
        }

        var genreSpec = new GenreByNameQuery(request.GenreName.Trim().ToUpperInvariant());
        var genre = await genresRepository.FirstOrDefaultAsync(genreSpec, cancellationToken);
        if (genre is null)
        {
            throw new BadOperationException("Genre not found");
        }

        book.SetTitle(request.Title);
        book.SetAuthor(author);
        book.SetGenre(genre);

        await booksRepository.UpdateAsync(book, cancellationToken);
        return book;
    }
}