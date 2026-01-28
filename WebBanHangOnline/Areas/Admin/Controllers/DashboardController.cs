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
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalRevenue = await _context.Orders
                .Where(o => o.Status == "Paid" || o.Status == "Completed")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.TotalUsers = await _context.Users.CountAsync();

            // Doanh thu 7 ngày gần nhất
            var data = await _context.Orders
                .Where(o => o.Status == "Paid" || o.Status == "Completed")
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .Take(7)
                .ToListAsync();

            ViewBag.ChartLabels = data.Select(x => x.Date.ToString("dd/MM")).ToList();
            ViewBag.ChartData = data.Select(x => x.Revenue).ToList();

            return View();
        }
    }
}