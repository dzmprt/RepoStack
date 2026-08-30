
using Books.Domain;
using RepoStack;

namespace Books.Application.DbQueries;

public class AuthorByFreeTextQuery(string freeText, bool? isDeleted = false, int? limit = 10, int? offset = null) :
    Query<Author>(author =>
        (isDeleted == null || isDeleted == author.IsDeleted) &&
        (author.FirstName.Contains(freeText) ||
        author.LastName.Contains(freeText)),
        [new QueryOrder<Author, int>(a => a.AuthorId, false)],
        limit,
        offset
    );