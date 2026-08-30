using Books.Domain;
using RepoStack;

namespace Books.Application.DbQueries;

public class BookByIdQuery(int bookId, bool? isPublished = true) :
    Query<Book>(book =>
        (isPublished == null || isPublished == book.IsPublished) &&
        !book.Author.IsDeleted &&
        book.BookId == bookId);