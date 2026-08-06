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
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public class OrderStatusReportViewModel
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}
