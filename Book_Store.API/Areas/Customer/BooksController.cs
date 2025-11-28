using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Book_Store.API.Areas.Customer
{
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("Customer")]
    public class BooksController : ControllerBase
    {
        private readonly IRepository<Book> _bookRepositery;
        private readonly IRepository<Favorite> _favoriteRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<Order> _orderRepository;


        public BooksController(IRepository<Book> bookRepositery, IRepository<Favorite> favoriteRepository, IRepository<Order> orderRepository, UserManager<ApplicationUser> userManager)
        {
            _bookRepositery = bookRepositery;
            _favoriteRepository = favoriteRepository;
            _orderRepository = orderRepository;
            _userManager = userManager;
        }

        [HttpGet("Home")]
        public async Task<IActionResult> Home(string? search)
        {
            var books = await _bookRepositery.GetAsync();

            if (search is not null)
            {
                var searchBook = books.Where(m => m.Title.Contains(search));
            }

            var bestSellers = books.OrderByDescending(m => m.Sold).Take(8).ToList();

            var recommended = books.OrderByDescending(m => m.Rating).Take(8).ToList();

            var flashSales = books.Where(m => m.Discount >= 30).OrderByDescending(m => m.Discount).Take(8).ToList();

            return Ok(new
            {
                BestSellers = bestSellers,
                Recommended = recommended,
                FlashSales = flashSales
            });
        }

        [HttpGet("Books")]
        public async Task<IActionResult> Books(int? categoryId)
        {
            var books = await _bookRepositery.GetAsync(includes: [e => e.Category]);

            var totalBooks = books.Count();

            var categoriesCount = books.GroupBy(b => new { b.CategoryId, b.Category.Name }).Select(e => new
            {
                CategoryId = e.Key.CategoryId,
                CategoryName = e.Key.Name,
                Count = e.Count()
            }).ToList();

            var filteredBooks = books;
            if (categoryId is not null)
            {
                filteredBooks = books.Where(m => m.CategoryId == categoryId);
            }

            return Ok(new
            {
                Books = filteredBooks,
                Categories = categoriesCount,
                Total = totalBooks
            });

        }


        [HttpGet("GetFavorites")]
        public async Task<IActionResult> GetFavorites()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new
                {
                    Message = "User is not authenticated."
                });

            var favorites = await _favoriteRepository.GetAsync(e => e.UserId == user.Id, includes: [f => f.Book]);

            return Ok(favorites);
        }


        [HttpPost("Favorite")]
        public async Task<IActionResult> Favorite(int bookId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new
                {
                    Message = "User is not authenticated."
                });

            var exist = await _favoriteRepository.GetOneAsync(f => f.BookId == bookId && f.UserId == user.Id);
            if (exist != null)
                return BadRequest(new
                {
                    message = "Already in favorites!"
                });

            await _favoriteRepository.AddAsync(new Favorite
            {
                BookId = bookId,
                UserId = user.Id
            });

            await _favoriteRepository.CommitAsync();
            return CreatedAtAction(nameof(GetFavorites), new
            {
                message = "Added to favorites.",

            });

        }


        [HttpDelete("{bookId}")]
        public async Task<IActionResult> RemoveFavorite(int bookId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new
                {
                    Message = "User is not authenticated."
                });

            var favorite = await _favoriteRepository.GetOneAsync(f => f.BookId == bookId && f.UserId == user.Id);
            if (favorite == null)
                return BadRequest(new
                {
                    message = "Not found in favorites!"
                });

            _favoriteRepository.Delete(favorite);
            await _favoriteRepository.CommitAsync();

            return Ok(new
            {
                message = "Removed from favorites."
            });
        }


        [HttpGet("Orders")]
        public async Task<IActionResult> Orders(string? status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new
                {
                    Message = "User is not authenticated."
                });

            var orders = await _orderRepository.GetAsync(o => o.UserId == user.Id && o.Status == status);

            return Ok(orders);
        }

    }
}
