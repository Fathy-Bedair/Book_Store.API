namespace Book_Store.API.Repositories
{
    public class BookRepository : Repository<Book> , IBookRepository
    {
        private ApplicationDbContext _context ;
        public BookRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<Book> books, CancellationToken cancellationToken = default)
        {
            await _context.AddRangeAsync(books, cancellationToken);
        }
    }
}
