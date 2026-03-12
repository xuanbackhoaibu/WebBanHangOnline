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
            // ==================== TRẠNG THÁI ĐƯỢC TÍNH DOANH THU ====================
            var validStatus = new[] { "Paid", "Completed", "Đã giao", "Delivered" };

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

            ViewBag.TodayOrders = await _context.Orders
                .Where(o => o.OrderDate >= today && o.OrderDate < tomorrow)
                .CountAsync();

            ViewBag.TodayRevenue = await _context.Orders
                .Where(o => validStatus.Contains(o.Status)
                        && o.OrderDate >= today
                        && o.OrderDate < tomorrow)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // ==================== DOANH THU 7 NGÀY ====================
            var revenueLabels = new List<string>();
            var revenueData = new List<decimal>();

            for (int i = 6; i >= 0; i--)
            {
                var start = DateTime.Today.AddDays(-i);
                var end = start.AddDays(1);

                var revenue = await _context.Orders
                    .Where(o => validStatus.Contains(o.Status)
                            && o.OrderDate >= start
                            && o.OrderDate < end)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                revenueLabels.Add(start.ToString("dd/MM"));
                revenueData.Add(revenue);
            }

            ViewBag.ChartLabels = revenueLabels;
            ViewBag.ChartData = revenueData;

            // ==================== PHÂN BỐ TRẠNG THÁI ====================
            var orderStatusStats = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
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
                    o.OrderDate
                })
                .ToListAsync();

            ViewBag.RecentOrders = recentOrders;

            return View();
        }
    }
}
