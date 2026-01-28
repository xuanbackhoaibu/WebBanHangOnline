using System;
using System.ComponentModel.DataAnnotations;

namespace WebBanHangOnline.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(150, ErrorMessage = "Tiêu đề không được dài quá 150 ký tự")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung thông báo")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Loại thông báo")]
        [StringLength(20)]
        public string Type { get; set; } = "normal";  // flash, promo, news

        [Display(Name = "Hiển thị trên User")]
        public bool IsActive { get; set; } = true;   // Admin có thể tắt hiển thị

        [Display(Name = "Ưu tiên hiển thị")]
        public int Priority { get; set; } = 0;      // Giá trị càng cao càng được ưu tiên
    }
}