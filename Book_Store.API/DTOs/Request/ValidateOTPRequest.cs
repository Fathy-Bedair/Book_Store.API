using System.ComponentModel.DataAnnotations;

namespace Book_Store.API.DTOs.Request
{
    public class ValidateOTPRequest
    {
        [Required]

        public string OTP { get; set; } = string.Empty;

        public string ApplicationUserId { get; set; } = string.Empty;
    }
}
