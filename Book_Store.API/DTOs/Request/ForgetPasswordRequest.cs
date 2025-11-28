using System.ComponentModel.DataAnnotations;

namespace Book_Store.API.DTOs.Request
{
    public class ForgetPasswordRequest
    {
        [Required]
        public string UserNameOREmail { get; set; } = string.Empty;
    }
}
