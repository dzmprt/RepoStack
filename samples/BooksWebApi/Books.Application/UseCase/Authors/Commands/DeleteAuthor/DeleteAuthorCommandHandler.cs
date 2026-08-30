using Books.Application.DbQueries;
using Books.Application.Exceptions;
using Books.Domain;
using MitMediator;
using RepoStack;

namespace Books.Application.UseCase.Authors.Commands.DeleteAuthor;

/// <summary>
/// Handler for <see cref="DeleteAuthorCommand"/>.
/// </summary>
internal sealed class DeleteAuthorCommandHandler(IRepository<Author> authorRepository) : IRequestHandler<DeleteAuthorCommand>
{
    /// <inheritdoc/>
    public async ValueTask<Unit> HandleAsync(DeleteAuthorCommand command, CancellationToken cancellationToken)
    {
        var query = new AuthorByIdQuery(command.AuthorId);

        var author = await authorRepository.FirstOrDefaultAsync(query, cancellationToken);
        if (author is null)
        {
            throw new NotFoundException();
        }

        author.Delete();
        await authorRepository.UpdateAsync(author, cancellationToken);
        return Unit.Value;
    }
}