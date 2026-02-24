using System.ComponentModel.DataAnnotations;

namespace WebBanHangOnline.Models
{
    public class WishlistItem
    {
        public int WishlistItemId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int ProductId { get; set; }

        public Product? Product { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}