using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;

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
                var validStatus = new[] { 
                    "Paid", 
                    "Completed", 
                    "Đã giao", 
                    "Delivered",
                    "Đã hoàn thành",
                    "Hoàn thành",
                    "Đã thanh toán"
                };

                // ==================== THỐNG KÊ CƠ BẢN ====================
                ViewBag.TotalOrders = await _context.Orders.CountAsync();

                ViewBag.TotalRevenue = await _context.Orders
                    .Where(o => validStatus.Contains(o.Status))
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
                    .Where(o => validStatus.Contains(o.Status))
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
                        .Where(o => validStatus.Contains(o.Status))
                        .Sum(o => o.TotalAmount);

                    revenueLabels.Add(start.ToString("dd/MM"));
                    revenueData.Add(revenue);

                    // Lưu chi tiết để debug
                    revenueDetail.Add(new
                    {
                        Ngay = start.ToString("dd/MM/yyyy"),
                        SoDon = ordersInDay.Count,
                        DonHopLe = ordersInDay.Count(o => validStatus.Contains(o.Status)),
                        DoanhThu = revenue
                    });
                }

                ViewBag.ChartLabels = revenueLabels;
                ViewBag.ChartData = revenueData;
                ViewBag.RevenueDetail = revenueDetail; // Thêm để debug

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

                // ==================== ĐƠN HÀNG GẦN NHẤT ====================
                var recentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
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
                        OrderDate = o.OrderDate,
                        IsValidRevenue = validStatus.Contains(o.Status) // Đánh dấu đơn được tính doanh thu
                    })
                    .ToListAsync();

                ViewBag.RecentOrders = recentOrders;

                // ==================== THỐNG KÊ BỔ SUNG ====================
                // Tổng đơn theo trạng thái
                ViewBag.TotalPending = await _context.Orders.CountAsync(o => o.Status.Contains("Chờ") || o.Status.Contains("Pending"));
                ViewBag.TotalProcessing = await _context.Orders.CountAsync(o => o.Status.Contains("xử lý") || o.Status.Contains("Processing"));
                ViewBag.TotalCompleted = await _context.Orders.CountAsync(o => validStatus.Contains(o.Status));
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
                ViewBag.RecentOrders = new List<object>();
                
                return View();
            }
        }
    }
}