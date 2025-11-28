namespace Book_Store.API.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public string Address { get; set; }

        public List<OrderItem> Items { get; set; }
    }
}
