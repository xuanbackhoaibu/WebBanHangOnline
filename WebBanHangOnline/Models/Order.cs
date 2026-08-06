using WebBanHangOnline.Models;

public class Order
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public string ShippingAddress { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountCode { get; set; }

    public string Status { get; set; } = OrderStatuses.Pending;
    public string? AdminNote { get; set; }
    
    public string PaymentMethod { get; set; } = PaymentMethods.Cod;
    public string PaymentStatus { get; set; } = PaymentStatuses.Unpaid;

    // 🔹 Thêm trường PaymentDate
    public DateTime? PaymentDate { get; set; }
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
    
    
}
