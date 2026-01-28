using System.ComponentModel.DataAnnotations;

namespace WebBanHangOnline.Models
{
    public class SupportRequest
    {
        public int SupportRequestId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        public string Message { get; set; } = string.Empty;

        public string Status { get; set; } = "New"; // New, InProgress, Done

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}