using System.ComponentModel.DataAnnotations;

namespace WebBanHangOnline.Models
{
    public class SupportFaq
    {
        public int SupportFaqId { get; set; }

        [Required]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}