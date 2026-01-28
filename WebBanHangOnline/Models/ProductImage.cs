namespace WebBanHangOnline.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        // FK
        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}