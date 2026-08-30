using Books.Application.Exceptions;
using Books.Domain;
using MitMediator;
using Books.Application.DbQueries;
using RepoStack;

namespace Books.Application.UseCase.Authors.Queries.GetAuthor;

/// <summary>
/// Handler for <see cref="GetAuthorQuery"/>.
/// </summary>
internal sealed class GetAuthorQueryHandler(IProvider<Author> authorProvider) : IRequestHandler<GetAuthorQuery, Author>
{
    /// <inheritdoc/>
    public async ValueTask<Author> HandleAsync(GetAuthorQuery query, CancellationToken cancellationToken)
    {
        var dbQuery = new AuthorByIdQuery(query.AuthorId);

        var author = await authorProvider.FirstOrDefaultAsync(dbQuery, cancellationToken);
        if (author is null)
        {
            throw new NotFoundException();
        }

        return author;
    }
}