using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHangOnline.Models;

public class DiscountCode
{
    public int Id { get; set; }

    [Required]
    [MaxLength(32)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(16)]
    public string DiscountType { get; set; } = DiscountTypes.Percent;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MinimumOrderAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaximumDiscountAmount { get; set; }

    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public static class DiscountTypes
{
    public const string Percent = "Percent";
    public const string FixedAmount = "FixedAmount";

    public static readonly string[] All =
    {
        Percent,
        FixedAmount
    };
}
