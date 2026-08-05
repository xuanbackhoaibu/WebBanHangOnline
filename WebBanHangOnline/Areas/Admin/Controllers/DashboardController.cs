using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // ==================== TRẠNG THÁI ĐƯỢC TÍNH DOANH THU ====================
                // Mở rộng thêm các trạng thái tính doanh thu
                var validStatus = OrderStatuses.RevenueStatuses;

                // ==================== THỐNG KÊ CƠ BẢN ====================
                ViewBag.TotalOrders = await _context.Orders.CountAsync();

                ViewBag.TotalRevenue = await _context.Orders
                    .Where(o => validStatus.Contains(o.Status) || o.PaymentStatus == PaymentStatuses.Paid)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                ViewBag.TotalUsers = await _context.Users.CountAsync();

                ViewBag.TotalProducts = await _context.Products.CountAsync();

                // ==================== THỐNG KÊ HÔM NAY ====================
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // Lấy tất cả đơn hàng hôm nay
                var todayOrders = await _context.Orders
                    .Where(o => o.OrderDate >= today && o.OrderDate < tomorrow)
                    .ToListAsync();

                ViewBag.TodayOrders = todayOrders.Count;
                
                ViewBag.TodayRevenue = todayOrders
                    .Where(o => validStatus.Contains(o.Status) || o.PaymentStatus == PaymentStatuses.Paid)
                    .Sum(o => o.TotalAmount);

                // ==================== DOANH THU 7 NGÀY ====================
                var revenueLabels = new List<string>();
                var revenueData = new List<decimal>();
                var revenueDetail = new List<object>(); // Để debug

                for (int i = 6; i >= 0; i--)
                {
                    var start = DateTime.Today.AddDays(-i);
                    var end = start.AddDays(1);

                    // Lấy tất cả đơn hàng trong ngày
                    var ordersInDay = await _context.Orders
                        .Where(o => o.OrderDate >= start && o.OrderDate < end)
                        .ToListAsync();

                    // Tính doanh thu từ các đơn hợp lệ
                    var revenue = ordersInDay
                        .Where(o => validStatus.Contains(o.Status) || o.PaymentStatus == PaymentStatuses.Paid)
                        .Sum(o => o.TotalAmount);

                    revenueLabels.Add(start.ToString("dd/MM"));
                    revenueData.Add(revenue);

                    // Lưu chi tiết để debug
                    revenueDetail.Add(new
                    {
                        Ngay = start.ToString("dd/MM/yyyy"),
                        SoDon = ordersInDay.Count,
                        DonHopLe = ordersInDay.Count(o => validStatus.Contains(o.Status) || o.PaymentStatus == PaymentStatuses.Paid),
                        DoanhThu = revenue
                    });
                }

                ViewBag.ChartLabels = revenueLabels;
                ViewBag.ChartData = revenueData;
                ViewBag.RevenueDetail = revenueDetail; // Thêm để debug
                ViewBag.CurrentRevenueMonth = DateTime.Today.ToString("yyyy-MM");

                // ==================== PHÂN BỐ TRẠNG THÁI ====================
                var orderStatusStats = await _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                ViewBag.OrderStatusLabels = orderStatusStats.Select(x => x.Status).ToList();
                ViewBag.OrderStatusData = orderStatusStats.Select(x => x.Count).ToList();

                var paymentStatusStats = await _context.Orders
                    .GroupBy(o => o.PaymentStatus)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                ViewBag.PaymentStatusLabels = paymentStatusStats.Select(x => x.Status).ToList();
                ViewBag.PaymentStatusData = paymentStatusStats.Select(x => x.Count).ToList();

                var paidOrderDetails = await _context.OrderDetails
                    .Include(od => od.Order)
                    .Include(od => od.ProductVariant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Category)
                    .Where(od => validStatus.Contains(od.Order.Status) || od.Order.PaymentStatus == PaymentStatuses.Paid)
                    .ToListAsync();

                var categorySales = paidOrderDetails
                    .GroupBy(od => od.ProductVariant.Product.Category != null
                        ? od.ProductVariant.Product.Category.Name
                        : "Khác")
                    .Select(g => new
                    {
                        Category = g.Key,
                        Revenue = g.Sum(od => od.Price * od.Quantity)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(6)
                    .ToList();

                ViewBag.CategorySalesLabels = categorySales.Select(x => x.Category).ToList();
                ViewBag.CategorySalesData = categorySales.Select(x => x.Revenue).ToList();

                var lowStockVariants = await _context.ProductVariants
                    .Include(v => v.Product)
                    .Where(v => v.Product.IsActive)
                    .OrderBy(v => v.Stock)
                    .Take(6)
                    .Select(v => new
                    {
                        ProductName = v.Product.Name,
                        v.Size,
                        v.Color,
                        v.Stock
                    })
                    .ToListAsync();

                ViewBag.LowStockVariants = lowStockVariants;

                // ==================== ĐƠN HÀNG GẦN NHẤT ====================
                var recentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .Select(o => new
                    {
                        o.Id,
                        OrderCode = "ORD" + o.Id.ToString("D6"),
                        CustomerName = o.User != null
                            ? (o.User.FullName ?? o.User.UserName)
                            : "Khách vãng lai",
                        CustomerEmail = o.User != null ? o.User.Email : "",
                        o.TotalAmount,
                        o.Status,
                        o.PaymentStatus,
                        OrderDate = o.OrderDate,
                        IsValidRevenue = validStatus.Contains(o.Status) || o.PaymentStatus == PaymentStatuses.Paid
                    })
                    .ToListAsync();

                ViewBag.RecentOrders = recentOrders;

                // ==================== THỐNG KÊ BỔ SUNG ====================
                // Tổng đơn theo trạng thái
                ViewBag.TotalPending = await _context.Orders.CountAsync(o => o.Status.Contains("Chờ") || o.Status.Contains("Pending"));
                ViewBag.TotalProcessing = await _context.Orders.CountAsync(o => o.Status.Contains("xử lý") || o.Status.Contains("Processing"));
                ViewBag.TotalCompleted = await _context.Orders.CountAsync(o => validStatus.Contains(o.Status) || o.PaymentStatus == PaymentStatuses.Paid);
                ViewBag.TotalCancelled = await _context.Orders.CountAsync(o => o.Status.Contains("hủy") || o.Status.Contains("Cancel"));

                return View();
            }
            catch (Exception ex)
            {
                // Log lỗi ra console
                Console.WriteLine($"Lỗi Dashboard: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                // Trả về view với dữ liệu mặc định
                ViewBag.TotalOrders = 0;
                ViewBag.TotalRevenue = 0;
                ViewBag.TotalUsers = 0;
                ViewBag.TotalProducts = 0;
                ViewBag.TodayOrders = 0;
                ViewBag.TodayRevenue = 0;
                ViewBag.ChartLabels = new List<string>();
                ViewBag.ChartData = new List<decimal>();
                ViewBag.OrderStatusLabels = new List<string>();
                ViewBag.OrderStatusData = new List<int>();
                ViewBag.PaymentStatusLabels = new List<string>();
                ViewBag.PaymentStatusData = new List<int>();
                ViewBag.CategorySalesLabels = new List<string>();
                ViewBag.CategorySalesData = new List<decimal>();
                ViewBag.LowStockVariants = new List<object>();
                ViewBag.RecentOrders = new List<object>();
                
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData(string revenueMode = "7days", string? month = null)
        {
            var validStatus = OrderStatuses.RevenueStatuses;
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var revenueLabels = new List<string>();
            var revenueData = new List<decimal>();

            if (string.Equals(revenueMode, "month", StringComparison.OrdinalIgnoreCase))
            {
                var selectedMonth = TryParseMonth(month) ?? new DateTime(today.Year, today.Month, 1);
                var daysInMonth = DateTime.DaysInMonth(selectedMonth.Year, selectedMonth.Month);

                for (int day = 1; day <= daysInMonth; day++)
                {
                    var start = new DateTime(selectedMonth.Year, selectedMonth.Month, day);
                    var end = start.AddDays(1);

                    var revenue = await _context.Orders
                        .Where(o => o.OrderDate >= start && o.OrderDate < end)
                        .Where(o => validStatus.Contains(o.Status) || o.PaymentStatus == PaymentStatuses.Paid)
                        .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                    revenueLabels.Add(start.ToString("dd/MM"));
                    revenueData.Add(revenue);
                }
            }
            else
            {
                for (int i = 6; i >= 0; i--)
                {
                    var start = DateTime.Today.AddDays(-i);
                    var end = start.AddDays(1);

                    var revenue = await _context.Orders
                        .Where(o => o.OrderDate >= start && o.OrderDate < end)
                        .Where(o => validStatus.Contains(o.Status) || o.PaymentStatus == PaymentStatuses.Paid)
                        .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                    revenueLabels.Add(start.ToString("dd/MM"));
                    revenueData.Add(revenue);
                }
            }

            var statusStats = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var paymentStats = await _context.Orders
                .GroupBy(o => o.PaymentStatus)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var paidOrderDetails = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Category)
                .Where(od => validStatus.Contains(od.Order.Status) || od.Order.PaymentStatus == PaymentStatuses.Paid)
                .ToListAsync();

            var categorySales = paidOrderDetails
                .GroupBy(od => od.ProductVariant.Product.Category != null
                    ? od.ProductVariant.Product.Category.Name
                    : "Khác")
                .Select(g => new
                {
                    Label = g.Key,
                    Revenue = g.Sum(od => od.Price * od.Quantity)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(6)
                .ToList();

            return Json(new
            {
                success = true,
                totalOrders = await _context.Orders.CountAsync(),
                totalRevenue = await _context.Orders
                    .Where(o => validStatus.Contains(o.Status) || o.PaymentStatus == PaymentStatuses.Paid)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
                totalUsers = await _context.Users.CountAsync(),
                totalProducts = await _context.Products.CountAsync(),
                todayOrders = await _context.Orders.CountAsync(o => o.OrderDate >= today && o.OrderDate < tomorrow),
                revenueLabels,
                revenueData,
                statusLabels = statusStats.Select(x => x.Label).ToList(),
                statusData = statusStats.Select(x => x.Count).ToList(),
                paymentLabels = paymentStats.Select(x => x.Label).ToList(),
                paymentData = paymentStats.Select(x => x.Count).ToList(),
                categoryLabels = categorySales.Select(x => x.Label).ToList(),
                categoryData = categorySales.Select(x => x.Revenue).ToList()
            });
        }

        private static DateTime? TryParseMonth(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return DateTime.TryParseExact(
                value,
                "yyyy-MM",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var month)
                ? new DateTime(month.Year, month.Month, 1)
                : null;
        }
    }
}
