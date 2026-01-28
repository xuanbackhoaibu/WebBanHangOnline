using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace WebBanHangOnline.Models
{
    public class Product
    {
        public int ProductId { get; set; }

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

        // ===============================
        // TẠO SLUG TỪ TÊN SẢN PHẨM
        // ===============================
        public void GenerateSlug()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return;

            var slug = Name.ToLower().Trim();

            // bỏ dấu tiếng Việt
            slug = RemoveVietnameseTone(slug);

            // bỏ ký tự đặc biệt
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

            // đổi space -> -
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
