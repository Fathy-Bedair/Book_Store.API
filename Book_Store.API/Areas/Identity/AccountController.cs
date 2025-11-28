using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace Book_Store.API.Areas.Identity
{
    [Route("[Area]/[controller]")]
    [ApiController]
    [Area("Identity")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IRepository<ApplicationUserOTP> _applicationUserOTPRepository;


        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender, IRepository<ApplicationUserOTP> applicationUserOTPRepository)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _applicationUserOTPRepository = applicationUserOTPRepository;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterRequest registerRequest)
        {

            var user = new ApplicationUser()
            {
                FirstName = registerRequest.FirstName,
                LastName = registerRequest.LastName,
                Email = registerRequest.Email,
                UserName = registerRequest.UserName,
            };

            var result = await _userManager.CreateAsync(user, registerRequest.Password);

            if (!result.Succeeded)
            {
                
                return BadRequest(result.Errors);
            }
            // Send Email Confirmation Link
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(nameof(ConfirmEmail), "Account", new { area = "Identity", token, userId = user.Id }, Request.Scheme);

            await _emailSender.SendEmailAsync(registerRequest.Email, "Book Store - Confirm Your Email!"
                , $"<h1>Confirm Your Email By Clicking <a href='{link}'>Here</a></h1>");

            await _userManager.AddToRoleAsync(user, SD.CUSTOMER_ROLE);

            return Ok(new
            {
                msg = "Create Account Successfully"
            });
        }

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                return NotFound(new
                {
                    msg = "Invalid User"
                });

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
                return BadRequest(new
                {
                    msg = "Email Confirmation Failed"
                });
            else
                return Ok(new
                {
                    msg = "Email Confirmed Successfully"
                });
        }


        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequest loginRequest)
        {
            

            var user = await _userManager.FindByNameAsync(loginRequest.UserNameOREmail) ?? await _userManager.FindByEmailAsync(loginRequest.UserNameOREmail);

            if (user is null)
            {
                return NotFound(new ErrorModelResponse
                {
                    Code = "Invalid Cred.",
                    Description = "Invalid User Name / Email OR Password"
                });
            }

            var result = await _signInManager.PasswordSignInAsync(user, loginRequest.Password, loginRequest.RememberMe, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    return BadRequest(new ErrorModelResponse
                    {
                        Code = "Too many attemps",
                        Description = "Too many attemps, try again after 5 min"
                    });
                else if (!user.EmailConfirmed)
                    return BadRequest(new ErrorModelResponse
                    {
                        Code = "Confirm Your Email",
                        Description = "Please Confirm Your Email First!!"
                    });
                else
                    return NotFound(new ErrorModelResponse
                    {
                        Code = "Invalid Cred.",
                        Description = "Invalid User Name / Email OR Password"
                    });
               
            }
            return Ok(new
            {
                msg = "Login Successfully"
            });

        }


        [HttpPost("ForgetPassword")]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordRequest forgetPasswordRequest)
        {
            

            var user = await _userManager.FindByNameAsync(forgetPasswordRequest.UserNameOREmail) ?? await _userManager.FindByEmailAsync(forgetPasswordRequest.UserNameOREmail);

            if (user is null)
            {
                return NotFound(new ErrorModelResponse
                {
                    Code = "Invalid Cred.",
                    Description = "Invalid User Name / Email OR Password"
                });
            }

            var userOTPs = await _applicationUserOTPRepository.GetAsync(e => e.ApplicationUserId == user.Id);

            var totalOTPs = userOTPs.Count(e => (DateTime.UtcNow - e.CreateAt).TotalHours < 24);

            if (totalOTPs > 3)
            {
                return BadRequest(new ErrorModelResponse
                {
                    Code = "Too many attemps",
                    Description = "Too many attemps, try again later"
                });
            }

            var otp = new Random().Next(1000, 9999).ToString(); // 1000 - 9999

            await _applicationUserOTPRepository.AddAsync(new()
            {
                Id = Guid.NewGuid().ToString(),
                ApplicationUserId = user.Id,
                CreateAt = DateTime.UtcNow,
                IsValid = true,
                OTP = otp,
                ValidTo = DateTime.UtcNow.AddDays(1),
            });
            await _applicationUserOTPRepository.CommitAsync();

            await _emailSender.SendEmailAsync(user.Email!, "Book Store - Reset Your Password"
                , $"<h1>Use This OTP: {otp} To Reset Your Account. Don't share it.</h1>");

            return CreatedAtAction("ValidateOTP", new { userId = user.Id });
        }


        [HttpPost("ValidateOTP")]
        public async Task<IActionResult> ValidateOTP(ValidateOTPRequest validateOTPRequest)
        {

            var result = await _applicationUserOTPRepository.GetOneAsync(e => e.ApplicationUserId == validateOTPRequest.ApplicationUserId && e.OTP == validateOTPRequest.OTP && e.IsValid);

            if (result is null)
            {
                return CreatedAtAction("ValidateOTP", new { userId = validateOTPRequest.ApplicationUserId });
            }

            return CreatedAtAction("ValidateOTP", new { userId = validateOTPRequest.ApplicationUserId });
        }

        
        [HttpPost("ResendOTP")]
        public async Task<IActionResult> ResendOTP(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return NotFound();

            var userOTPs = await _applicationUserOTPRepository.GetAsync(e => e.ApplicationUserId == user.Id);

            var totalOTPs = userOTPs.Count(e => (DateTime.UtcNow - e.CreateAt).TotalHours < 24);

            if (totalOTPs > 3)
            {
                return BadRequest(new ErrorModelResponse
                {
                    Code = "Too many attemps",
                    Description = "Too many attemps, try again later"
                });
            }

            var existingOtps = await _applicationUserOTPRepository.GetOneAsync(e => e.ApplicationUserId == userId && e.IsValid);

            if (existingOtps is not null)
            {
                existingOtps.IsValid = false;
                _applicationUserOTPRepository.Update(existingOtps);
            }



            var newOtp = new Random().Next(1000, 9999).ToString();

            var newOtpEntity = new ApplicationUserOTP
            {
                Id = Guid.NewGuid().ToString(),
                ApplicationUserId = userId,
                OTP = newOtp,
                IsValid = true,
                CreateAt = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddDays(1)
            };

            await _applicationUserOTPRepository.AddAsync(newOtpEntity);
            await _applicationUserOTPRepository.CommitAsync();

            // Send OTP to user's email
            await _emailSender.SendEmailAsync(
                user.Email!,
                "Your OTP Code",
                $"<h2>Your new OTP code is: <strong>{newOtp}</strong></h2>"
            );

            return CreatedAtAction(nameof(ValidateOTP), new { userId });

        }


        [HttpPost("NewPassword")]
        public async Task<IActionResult> NewPassword(NewPasswordRequest newPasswordRequest)
        {
            var user = await _userManager.FindByIdAsync(newPasswordRequest.ApplicationUserId);

            if (user is null)
            {
                return NotFound(new ErrorModelResponse
                {
                    Code = "Invalid Cred.",
                    Description = "Invalid User Name / Email OR Password"
                });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(user, token, newPasswordRequest.Password);


            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);

            }

            return Ok();
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new
            {
                msg = "Logout Successfully"
            });
        }
    }
}
