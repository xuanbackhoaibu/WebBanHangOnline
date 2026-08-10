namespace WebBanHangOnline.Models.ViewModels;

public class ReportIndexViewModel
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscount { get; set; }
    public int TotalOrders { get; set; }
    public int PaidOrders { get; set; }
    public int UnpaidOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<ReportOrderRowViewModel> Orders { get; set; } = new();
    public List<DailyRevenueViewModel> DailyRevenue { get; set; } = new();
    public List<TopProductReportViewModel> TopProducts { get; set; } = new();
    public List<OrderStatusReportViewModel> Statuses { get; set; } = new();
}

public class DailyRevenueViewModel
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
}

public class TopProductReportViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public class OrderStatusReportViewModel
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ReportOrderRowViewModel
{
    public int Id { get; set; }
    public string OrderCode => $"ORD{Id:D6}";
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? DiscountCode { get; set; }
}
