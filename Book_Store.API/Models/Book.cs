using System.ComponentModel.DataAnnotations;

namespace Book_Store.API.Models
{
    public class Book
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(40)]
        [MinLength(3)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(1000)]
        [MinLength(10)]
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public double Price { get; set; }
        public double Discount { get; set; } = 0.0;
        public double Rating { get; set; }
        public string Image { get; set; } = string.Empty;
        public int Sold { get; set; } = 0;
        public bool InStock { get; set; }
        public int Quantity { get; set; }
        public int CategoryId { get; set; }
        // Relationships
        public Category Category { get; set; }

    }
}
