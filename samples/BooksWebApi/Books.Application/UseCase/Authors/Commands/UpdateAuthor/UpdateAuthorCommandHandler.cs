using Books.Application.Exceptions;
using Books.Domain;
using MitMediator;
using Books.Application.DbQueries;
using RepoStack;

namespace Books.Application.UseCase.Authors.Commands.UpdateAuthor;

/// <summary>
/// Handler for <see cref="UpdateAuthorCommand"/>.
/// </summary>
internal sealed class UpdateAuthorCommandHandler(IRepository<Author> authorRepository) : IRequestHandler<UpdateAuthorCommand, Author>
{
    /// <inheritdoc/>
    /// <returns>The updated author.</returns>
    public async ValueTask<Author> HandleAsync(UpdateAuthorCommand command, CancellationToken cancellationToken)
    {
        var query = new AuthorByIdQuery(command.AuthorId);

        var author = await authorRepository.FirstOrDefaultAsync(query, cancellationToken);
        if (author is null)
        {
            throw new NotFoundException();
        }
        author.UpdateFirstName(command.FirstName);
        author.UpdateLastName(command.LastName);
        await authorRepository.UpdateAsync(author, cancellationToken);
        return author;
    }
}