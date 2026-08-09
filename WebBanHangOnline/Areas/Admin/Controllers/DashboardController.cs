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

                var biSnapshot = await BuildBusinessIntelligenceSnapshot();
                ViewBag.RfmSegments = biSnapshot.RfmSegments;
                ViewBag.CartAbandonment = biSnapshot.CartAbandonment;
                ViewBag.CohortRows = biSnapshot.CohortRows;

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
                ViewBag.RfmSegments = new List<RfmSegmentViewModel>();
                ViewBag.CartAbandonment = new CartAbandonmentViewModel(0, 0, 0);
                ViewBag.CohortRows = new List<CohortRowViewModel>();
                
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
                categoryData = categorySales.Select(x => x.Revenue).ToList(),
                businessIntelligence = await BuildBusinessIntelligenceSnapshot()
            });
        }

        private async Task<BusinessIntelligenceSnapshot> BuildBusinessIntelligenceSnapshot()
        {
            var validStatus = OrderStatuses.RevenueStatuses;
            var paidOrders = await _context.Orders
                .AsNoTracking()
                .Where(order => validStatus.Contains(order.Status) || order.PaymentStatus == PaymentStatuses.Paid)
                .Select(order => new
                {
                    order.UserId,
                    order.OrderDate,
                    order.TotalAmount
                })
                .ToListAsync();

            var now = DateTime.Now;
            var rfmSegments = paidOrders
                .Where(order => !string.IsNullOrWhiteSpace(order.UserId))
                .GroupBy(order => order.UserId)
                .Select(group =>
                {
                    var recencyDays = (int)Math.Max(0, (now.Date - group.Max(order => order.OrderDate).Date).TotalDays);
                    var frequency = group.Count();
                    var monetary = group.Sum(order => order.TotalAmount);
                    var segment = ClassifyRfm(recencyDays, frequency, monetary);

                    return new
                    {
                        Segment = segment,
                        RecencyDays = recencyDays,
                        Frequency = frequency,
                        Monetary = monetary
                    };
                })
                .GroupBy(item => item.Segment)
                .Select(group => new RfmSegmentViewModel(
                    group.Key,
                    group.Count(),
                    group.Any() ? Math.Round(group.Average(item => item.RecencyDays), 1) : 0,
                    group.Any() ? Math.Round(group.Average(item => item.Frequency), 1) : 0,
                    group.Sum(item => item.Monetary)))
                .OrderByDescending(item => item.CustomerCount)
                .ToList();

            var cartUserIds = await _context.CartItems
                .AsNoTracking()
                .Where(item => item.CreatedAt <= now.AddHours(-1))
                .Select(item => item.UserId)
                .Distinct()
                .ToListAsync();

            var recentCheckoutUserIds = paidOrders
                .Where(order => order.OrderDate >= now.AddDays(-30))
                .Select(order => order.UserId)
                .Where(userId => !string.IsNullOrWhiteSpace(userId))
                .Distinct()
                .ToList();

            var abandonmentDenominator = cartUserIds.Count + recentCheckoutUserIds.Count;
            var abandonmentRate = abandonmentDenominator == 0
                ? 0
                : Math.Round(cartUserIds.Count * 100m / abandonmentDenominator, 1);

            var cohortRows = BuildCohortRows(paidOrders
                .Where(order => !string.IsNullOrWhiteSpace(order.UserId))
                .Select(order => new CustomerOrderPoint(order.UserId, new DateTime(order.OrderDate.Year, order.OrderDate.Month, 1)))
                .ToList());

            return new BusinessIntelligenceSnapshot(
                rfmSegments,
                new CartAbandonmentViewModel(cartUserIds.Count, recentCheckoutUserIds.Count, abandonmentRate),
                cohortRows);
        }

        private static string ClassifyRfm(int recencyDays, int frequency, decimal monetary)
        {
            if (recencyDays <= 30 && frequency >= 3 && monetary >= 1000000)
            {
                return "Khách hàng VIP";
            }

            if (recencyDays > 90 && frequency >= 2)
            {
                return "Có nguy cơ rời bỏ";
            }

            if (recencyDays <= 30 && frequency == 1)
            {
                return "Khách hàng mới";
            }

            if (frequency >= 2)
            {
                return "Khách hàng trung thành";
            }

            return "Cần nuôi dưỡng";
        }

        private static List<CohortRowViewModel> BuildCohortRows(List<CustomerOrderPoint> orders)
        {
            return orders
                .GroupBy(order => order.UserId)
                .Select(group => new
                {
                    UserId = group.Key,
                    FirstMonth = group.Min(order => order.Month),
                    Months = group.Select(order => order.Month).Distinct().ToHashSet()
                })
                .GroupBy(customer => customer.FirstMonth)
                .OrderByDescending(group => group.Key)
                .Take(6)
                .OrderBy(group => group.Key)
                .Select(group =>
                {
                    var customers = group.ToList();
                    var cohortSize = customers.Count;
                    var rates = Enumerable.Range(0, 6)
                        .Select(offset =>
                        {
                            var targetMonth = group.Key.AddMonths(offset);
                            var retained = customers.Count(customer => customer.Months.Contains(targetMonth));
                            return cohortSize == 0 ? 0 : Math.Round(retained * 100m / cohortSize, 1);
                        })
                        .ToList();

                    return new CohortRowViewModel(group.Key.ToString("MM/yyyy"), cohortSize, rates);
                })
                .ToList();
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

        private sealed record CustomerOrderPoint(string UserId, DateTime Month);

        public sealed record BusinessIntelligenceSnapshot(
            List<RfmSegmentViewModel> RfmSegments,
            CartAbandonmentViewModel CartAbandonment,
            List<CohortRowViewModel> CohortRows);

        public sealed record RfmSegmentViewModel(
            string Segment,
            int CustomerCount,
            double AverageRecencyDays,
            double AverageFrequency,
            decimal TotalMonetary);

        public sealed record CartAbandonmentViewModel(
            int AbandonedCartUsers,
            int RecentCheckoutUsers,
            decimal AbandonmentRate);

        public sealed record CohortRowViewModel(
            string CohortMonth,
            int CustomerCount,
            List<decimal> RetentionRates);
    }
}
