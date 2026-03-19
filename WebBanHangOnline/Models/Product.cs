using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace WebBanHangOnline.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string ImageUrl { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        // ===============================
        // SLUG SEO
        // ===============================
        [Required]
        [MaxLength(255)]
        public string Slug { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập giá sản phẩm")]
        [Range(1, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string Thumbnail { get; set; } = "/images/no-image.png";

        public bool IsActive { get; set; } = true;

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        // ======================================================
        // 🔥 FLASH SALE (THÊM MỚI – KHÔNG ẢNH HƯỞNG CODE CŨ)
        // ======================================================

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FlashSalePrice { get; set; }

        public DateTime? FlashSaleStart { get; set; }

        public DateTime? FlashSaleEnd { get; set; }

        [NotMapped]
        public bool IsFlashSaleActive =>
            FlashSalePrice.HasValue &&
            FlashSaleStart.HasValue &&
            FlashSaleEnd.HasValue &&
            DateTime.Now >= FlashSaleStart.Value &&
            DateTime.Now <= FlashSaleEnd.Value;

        [NotMapped]
        public decimal FinalPrice =>
            IsFlashSaleActive ? FlashSalePrice!.Value : Price;


        // ======================================================
        // ⭐ REVIEW (ĐÁNH GIÁ)
        // ======================================================

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        [NotMapped]
        public double AverageRating =>
            Reviews != null && Reviews.Any()
                ? Math.Round(Reviews.Average(r => r.Rating), 1)
                : 0;

        [NotMapped]
        public int ReviewCount =>
            Reviews?.Count ?? 0;


        // ======================================================
        // ❤️ WISHLIST
        // ======================================================

        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();


        // ===============================
        // TẠO SLUG TỪ TÊN SẢN PHẨM (GIỮ NGUYÊN)
        // ===============================
        public void GenerateSlug()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return;

            var slug = Name.ToLower().Trim();

            slug = RemoveVietnameseTone(slug);

            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

            slug = Regex.Replace(slug, @"\s+", "-");

            Slug = slug;
        }

        private static string RemoveVietnameseTone(string text)
        {
            string[] vietNamChar = {
                "aáàạảãâấầậẩẫăắằặẳẵ",
                "eéèẹẻẽêếềệểễ",
                "iíìịỉĩ",
                "oóòọỏõôốồộổỗơớờợởỡ",
                "uúùụủũưứừựửữ",
                "yýỳỵỷỹ",
                "dđ"
            };

            for (int i = 0; i < vietNamChar.Length; i++)
            {
                foreach (char c in vietNamChar[i])
                {
                    text = text.Replace(c.ToString(), ((char)('a' + i)).ToString());
                }
            }
            return text;
        }
    }
}