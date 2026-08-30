using Books.Domain;
using RepoStack;

namespace Books.Application.DbQueries;

public class GenreByNameQuery(string genreName) :
    Query<Genre>(genre => genre.GenreName == genreName);