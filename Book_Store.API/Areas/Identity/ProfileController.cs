using Book_Store.API.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Book_Store.API.Areas.Identity
{
    [Route("[area]/[controller]")]
    [ApiController]
    [Authorize]
    [Area("Identity")]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetInfo()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var userVM = user.Adapt<ApplicationUserResponse>();

            return Ok(userVM);
        }

        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile(ApplicationUserRequest applicationUserRequest)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var names = applicationUserRequest.FullName.Split(" ");

            user.PhoneNumber = applicationUserRequest.PhoneNumber;
            user.Address = applicationUserRequest.Address;
            user.FirstName = names[0];
            user.LastName = names[1];

            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                msg = "Profile updated successfully"
            });
        }

        [HttpPut("UpdatePassword")]
        public async Task<IActionResult> UpdatePassword(ApplicationUserRequest applicationUserRequest)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            if (applicationUserRequest.CurrentPassword is null || applicationUserRequest.NewPassword is null || applicationUserRequest.ConfirmNewPassword is null)
            {
                return BadRequest(new ErrorModelResponse
                {
                    Code = "Matching Current Password & New Password",
                    Description = "Must have a CurrentPassword & NewPassword value"
                });
            }

            var result = await _userManager.ChangePasswordAsync(user, applicationUserRequest.CurrentPassword, applicationUserRequest.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new
            {
                msg = "Password updated successfully"
            });
        }
    }

}

