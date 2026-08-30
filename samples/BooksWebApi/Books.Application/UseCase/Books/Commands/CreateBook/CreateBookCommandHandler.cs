using Books.Application.Exceptions;
using Books.Domain;
using MitMediator;
using Books.Application.DbQueries;
using RepoStack;

namespace Books.Application.UseCase.Books.Commands.CreateBook;

/// <summary>
/// Handler for <see cref="CreateBookCommand"/>
/// </summary>
public class CreateBookCommandHandler(
        IRepository<Book> booksRepository,
        IRepository<Author> authorsRepository,
        IRepository<Genre> genresRepository)
        : IRequestHandler<CreateBookCommand, Book>
{
    /// <inheritdoc/>
    /// <returns>The created book.</returns>
    public async ValueTask<Book> HandleAsync(CreateBookCommand request, CancellationToken cancellationToken)
    {
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

        var book = new Book(request.Title, author, genre);
        book.Publish();
        await booksRepository.AddAsync(book, cancellationToken);
        return book;
    }
}