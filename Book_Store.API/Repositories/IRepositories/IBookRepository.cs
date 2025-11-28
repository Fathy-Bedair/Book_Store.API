namespace Book_Store.API.Repositories.IRepositories
{
    public interface IBookRepository : IRepository<Book>
    {
        Task AddRangeAsync(IEnumerable<Book> books, CancellationToken cancellationToken = default);
    }
}