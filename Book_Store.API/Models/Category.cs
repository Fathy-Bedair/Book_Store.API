using System.ComponentModel.DataAnnotations;

namespace Book_Store.API.Models
{
    public class Category
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(30)]
        [MinLength(3)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(1000)]
        [MinLength(10)]
        public string? Description { get; set; }
        // Relationships
        public List<Book> Books { get; set; } = new List<Book>();

    }
}
