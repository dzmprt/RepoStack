using Books.Domain;
using RepoStack;

namespace Books.Application.DbQueries;

public class BookIsAvailableQuery() :
    Query<Book>(book =>
        book.IsPublished &&
        !book.Author.IsDeleted);