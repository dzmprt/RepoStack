using Books.Domain;
using RepoStack;

namespace Books.Application.DbQueries;

public class BookByFreeTextQuery(string? freeText = null, bool? isPublished = true, int? limit = 10, int? offset = null) :
    Query<Book>(
            book =>
                (isPublished == null || isPublished == book.IsPublished) &&
                !book.Author.IsDeleted &&
                (freeText == null || book.Author.FirstName.Contains(freeText) ||
                book.Author.LastName.Contains(freeText) ||
                book.Title.Contains(freeText) ||
                book.Genre.GenreName.Contains(freeText)),
        [new QueryOrder<Book, int>(a => a.BookId, false)],
        limit,
        offset
        );