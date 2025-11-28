using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Book_Store.API.Models
{
    public class Promotion
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        [ValidateNever]
        public Book Book { get; set; }
        public DateTime PublishAt { get; set; } = DateTime.UtcNow;
        public DateTime ValidTo { get; set; }
        public bool IsValid { get; set; } = true;

        public string Code { get; set; }
        public decimal Discount { get; set; }
    }
}
