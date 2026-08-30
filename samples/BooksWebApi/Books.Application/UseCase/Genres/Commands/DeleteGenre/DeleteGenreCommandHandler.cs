using Books.Application.DbQueries;
using Books.Application.Exceptions;
using Books.Domain;
using MitMediator;
using RepoStack;

namespace Books.Application.UseCase.Genres.Commands.DeleteGenre;

/// <summary>
/// Handler for <see cref="DeleteGenreCommand"/>.
/// </summary>
internal sealed class DeleteGenreCommandHandler(
    IRepository<Genre> genreRepository,
    IProvider<Book> bookProvider) : IRequestHandler<DeleteGenreCommand>
{
    /// <inheritdoc/>
    public async ValueTask<Unit> HandleAsync(DeleteGenreCommand command, CancellationToken cancellationToken)
    {
        var genreName = command.GenreName.Trim().ToUpperInvariant();
        var query = new GenreByNameQuery(genreName);

        var genre = await genreRepository.FirstOrDefaultAsync(query, cancellationToken);
        if (genre is null)
        {
            throw new NotFoundException();
        }

        var hasBooks = await bookProvider.AnyAsync(
            new Query<Book>(book => book.Genre.GenreName == genreName),
            cancellationToken);
        if (hasBooks)
        {
            throw new BadOperationException("Genre is used by books and cannot be deleted.");
        }

        await genreRepository.DeleteAsync(query, cancellationToken);
        return Unit.Value;
    }
}