using Books.Domain;
using RepoStack;

namespace Books.Application.DbQueries;

public class AuthorByIdQuery(int authorId, bool? isDeleted = false, int? limit = 10, int? offset = null) :
    Query<Author>(author =>
        (isDeleted == null || isDeleted == author.IsDeleted) &&
        author.AuthorId == authorId,
        null,
        limit,
        offset
    );