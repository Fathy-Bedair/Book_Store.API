using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Book_Store.API.Areas.Customer
{
    [Route("[area]/[controller]")]
    [ApiController]
    [Area("Customer")]
    public class CheckoutController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<Cart> _cartRepository;

        public CheckoutController(UserManager<ApplicationUser> userManager, IRepository<Cart> cartRepository)
        {
            _userManager = userManager;
            _cartRepository = cartRepository;
        }

        [HttpGet("success")]
        public async Task<IActionResult> Success(CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return Unauthorized(new
                {
                    Message = "User is not logged in."
                });

            var cart = await _cartRepository.GetAsync(e => e.ApplicationUserId == user.Id, includes: [e => e.Book, e => e.ApplicationUser]);

            foreach (var book in cart)
            {
                _cartRepository.Delete(book);
            }
            await _cartRepository.CommitAsync(cancellationToken);

            return Ok(new
            {
                Message = "Order placed successfully!"
            });
        }

        [HttpGet("cancel")]
        public IActionResult Cancel()
        {
            return Ok(new
            {
                Message = "Order was cancelled."
            });
        }
    }
}
