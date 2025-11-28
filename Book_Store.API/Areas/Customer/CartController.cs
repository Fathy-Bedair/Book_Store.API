using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace Book_Store.API.Areas.Customer
{
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("Customer")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<Cart> _cartRepository;
        private readonly IRepository<Promotion> _promotionRepository;
        private readonly IRepository<PromotionUsage> _promotionUsageRepository;
        private readonly IBookRepository _bookRepository;


        public CartController(UserManager<ApplicationUser> userManager, IRepository<Cart> cartRepository, IRepository<Promotion> promotionRepository, IBookRepository bookRepository, IRepository<PromotionUsage> promotionUsageRepository)
        {
            _bookRepository = bookRepository;
            _userManager = userManager;
            _cartRepository = cartRepository;
            _promotionRepository = promotionRepository;
            _promotionUsageRepository = promotionUsageRepository;
        }
        [HttpGet]
        public async Task<IActionResult> GetCart(string code)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return NotFound(new ErrorModelResponse
                {
                    Code = "Invalid Cred.",
                    Description = "Invalid User Name / Email OR Password"
                });
            }
            var cart = await _cartRepository.GetAsync(e => e.ApplicationUserId == user.Id, includes: [e => e.Book, e => e.ApplicationUser]);

            if (code is not null)
            {
                var promotion = await _promotionRepository.GetOneAsync(e => e.Code == code && e.IsValid);

                if (promotion is null)
                {
                    return NotFound(new ErrorModelResponse
                    {
                        Code = "Invalid Promo",
                        Description = "Invalid or expired promotion code!"
                    });
                }

                var alreadyUsed = await _promotionUsageRepository.GetOneAsync(u => u.UserId == user.Id && u.PromotionId == promotion.Id);

                if (alreadyUsed is not null)
                {
                    return BadRequest(new ErrorModelResponse
                    {
                        Code = "Already Used",
                        Description = "You have already used this promotion code!"
                    });
                }

                var result = cart.FirstOrDefault(e => e.BookId == promotion.BookId);

                if (result is not null)
                {
                    result.Price -= (decimal)result.Book.Price * (promotion.Discount / 100);
                    await _promotionUsageRepository.AddAsync(new PromotionUsage
                    {
                        PromotionId = promotion.Id,
                        UserId = user.Id,
                        UsedAt = DateTime.Now
                    });

                    await _promotionUsageRepository.CommitAsync();
                    await _cartRepository.CommitAsync();
                    return Ok(new
                    {
                        message = "Promotion code applied successfully!"
                    });
                }
                else
                {
                    return BadRequest(new ErrorModelResponse
                    {
                        Code = "Not Applicable",
                        Description = "This promotion code is not applicable to any items in your cart!"
                    });
                }
            }

            return Ok(cart);

        }


        [HttpPost]
        public async Task<IActionResult> AddToCart(int count, int bookId, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return NotFound(new ErrorModelResponse
                {
                    Code = "Invalid Cred.",
                    Description = "Invalid User Name / Email OR Password"
                });
            }

            var bookInDb = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.BookId == bookId);

            if (bookInDb is not null)
            {
                bookInDb.Count += count;
                await _cartRepository.CommitAsync(cancellationToken);

                return CreatedAtAction(nameof(GetCart), new
                {
                    message = "The number of books in the shopping cart has been successfully updated."
                });
            }

            await _cartRepository.AddAsync(new()
            {
                BookId = bookId,
                Count = count,
                ApplicationUserId = user.Id,
                Price = (decimal)(await _bookRepository.GetOneAsync(e => e.Id == bookId)!).Price
            }, cancellationToken: cancellationToken);
            await _cartRepository.CommitAsync(cancellationToken);

            return CreatedAtAction(nameof(GetCart), new
            {
                message = "Add book to cart successfully."
            });
        }


        [HttpPut("increment/{bookId}")]
        public async Task<IActionResult> IncrementBook(int bookId, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return NotFound(new ErrorModelResponse
                {
                    Code = "Invalid Cred.",
                    Description = "Invalid User Name / Email OR Password"
                });
            }

            var book = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.BookId == bookId);

            if (book is null)
                return NotFound(new ErrorModelResponse
                {
                    Code = "Not Found",
                    Description = "Book not found in cart."
                });

            book.Count += 1;
            await _cartRepository.CommitAsync(cancellationToken);
            return Ok(new
            {
                message = "The number of books in the shopping cart has been successfully updated."
            });

        }


        [HttpPut("decrement/{bookId}")]
        public async Task<IActionResult> DecrementBook(int bookId, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound(new ErrorModelResponse
                {
                    Code = "Invalid Cred.",
                    Description = "Invalid User Name / Email OR Password"
                });

            var book = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.BookId == bookId);

            if (book is null)
                return NotFound(new ErrorModelResponse
                {
                    Code = "Not Found",
                    Description = "Book not found in cart."
                });

            if (book.Count <= 1)
                _cartRepository.Delete(book);
            else
                book.Count -= 1;

            await _cartRepository.CommitAsync(cancellationToken);

            return Ok(new
            {
                message = "The number of books in the shopping cart has been successfully updated."
            });


        }


        [HttpDelete("{bookId}")]
        public async Task<IActionResult> DeleteBook(int bookId, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound(new ErrorModelResponse
                {
                    Code = "Invalid Cred.",
                    Description = "Invalid User Name / Email OR Password"
                });

            var book = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.BookId == bookId);

            if (book is null)
                return NotFound(new ErrorModelResponse
                {
                    Code = "Not Found",
                    Description = "Book not found in cart."
                });

            _cartRepository.Delete(book);
            await _cartRepository.CommitAsync(cancellationToken);

            return Ok(new
            {
                message = "Book removed from cart successfully."
            });

        }


        [HttpPost("Pay")]
        public async Task<IActionResult> Pay()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound(new ErrorModelResponse
                {
                    Code = "Invalid Cred.",
                    Description = "Invalid User Name / Email OR Password"
                });

            var cart = await _cartRepository.GetAsync(e => e.ApplicationUserId == user.Id, includes: [e => e.Book]);

            if (cart is null)
                return BadRequest(new ErrorModelResponse
                {
                    Code = "Empty Cart",
                    Description = "Your shopping cart is empty."
                });

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = $"{Request.Scheme}://{Request.Host}/customer/checkout/success",
                CancelUrl = $"{Request.Scheme}://{Request.Host}/customer/checkout/cancel",
            };

            foreach (var item in cart)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "egp",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Book.Title,
                            Description = item.Book.Description,
                        },
                        UnitAmount = (long)item.Price * 100,
                    },
                    Quantity = item.Count,
                });
            }

            var service = new SessionService();
            var session = service.Create(options);
            return Redirect(session.Url);
        }

    }
}
