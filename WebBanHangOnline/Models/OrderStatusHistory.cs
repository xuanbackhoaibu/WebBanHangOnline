using System.ComponentModel.DataAnnotations;

namespace WebBanHangOnline.Models;

public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    [MaxLength(32)]
    public string ChangeType { get; set; } = "OrderStatus";

    [MaxLength(64)]
    public string? FromValue { get; set; }

    [MaxLength(64)]
    public string? ToValue { get; set; }

    [MaxLength(256)]
    public string? Note { get; set; }

    [MaxLength(128)]
    public string ChangedBy { get; set; } = "Admin";

    public DateTime ChangedAt { get; set; } = DateTime.Now;
}
