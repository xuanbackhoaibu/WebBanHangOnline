namespace WebBanHangOnline.Models;

public class CheckoutVoucherViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DiscountText { get; set; } = string.Empty;
    public string ConditionText { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsSelected { get; set; }
    public string? DisabledReason { get; set; }
}
