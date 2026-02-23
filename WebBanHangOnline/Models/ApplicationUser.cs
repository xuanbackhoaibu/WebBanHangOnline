using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebBanHangOnline.Models
{
    public class ApplicationUser : IdentityUser
    {
        // =========================
        // THÔNG TIN CÁ NHÂN
        // =========================

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }   // Male / Female / Other


        // =========================
        // ĐỊA CHỈ (TÁCH CHI TIẾT)
        // =========================

        [MaxLength(100)]
        public string? Province { get; set; }

        [MaxLength(100)]
        public string? District { get; set; }

        [MaxLength(100)]
        public string? Ward { get; set; }

        [MaxLength(250)]
        public string? StreetAddress { get; set; }


        // =========================
        // AVATAR
        // =========================

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }


        // =========================
        // HỆ THỐNG
        // =========================

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }

        public bool IsActive { get; set; } = true;


        // =========================
        // HELPER PROPERTY (KHÔNG MAP DB)
        // =========================

        public string FullAddress =>
            $"{StreetAddress}, {Ward}, {District}, {Province}";
    }
}