using WebBanHangOnline.Models;

public class Order
{
    public int Id { get; set; }

    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public string ShippingAddress { get; set; }
    public string PhoneNumber { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = "Pending";

    // 🔹 Thêm trường PaymentDate
    public DateTime? PaymentDate { get; set; }
    public ICollection<OrderDetail> OrderDetails { get; set; }
    
    
}
