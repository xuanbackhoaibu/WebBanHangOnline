using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;
using WebBanHangOnline.Models.ViewModels;

namespace WebBanHangOnline.Areas.Admin.Controllers

{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 📊 Trang báo cáo
        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            var fromDate = from?.Date;
            var toExclusive = to?.Date.AddDays(1);

            var revenueQuery = _context.Orders
                .Where(o =>
                    OrderStatuses.RevenueStatuses.Contains(o.Status) ||
                    o.PaymentStatus == PaymentStatuses.Paid);

            if (fromDate.HasValue)
                revenueQuery = revenueQuery.Where(o => o.OrderDate >= fromDate.Value);

            if (toExclusive.HasValue)
                revenueQuery = revenueQuery.Where(o => o.OrderDate < toExclusive.Value);

            var orders = await revenueQuery
                .OrderByDescending(order => order.OrderDate)
                .ToListAsync();

            var orderIds = orders.Select(order => order.Id).ToList();

            var topProducts = orderIds.Any()
                ? await _context.OrderDetails
                    .Include(detail => detail.ProductVariant)
                    .ThenInclude(variant => variant.Product)
                    .Where(detail => orderIds.Contains(detail.OrderId))
                    .GroupBy(detail => detail.ProductVariant.Product.Name)
                    .Select(group => new TopProductReportViewModel
                    {
                        ProductName = group.Key,
                        Quantity = group.Sum(item => item.Quantity),
                        Revenue = group.Sum(item => item.Quantity * item.Price)
                    })
                    .OrderByDescending(item => item.Revenue)
                    .Take(5)
                    .ToListAsync()
                : new List<TopProductReportViewModel>();

            var model = new ReportIndexViewModel
            {
                TotalRevenue = orders.Sum(order => order.TotalAmount),
                TotalDiscount = orders.Sum(order => order.DiscountAmount),
                TotalOrders = orders.Count,
                PaidOrders = orders.Count(order => order.PaymentStatus == PaymentStatuses.Paid),
                UnpaidOrders = orders.Count(order => order.PaymentStatus == PaymentStatuses.Unpaid),
                AverageOrderValue = orders.Any() ? orders.Average(order => order.TotalAmount) : 0,
                From = fromDate?.ToString("yyyy-MM-dd"),
                To = to?.Date.ToString("yyyy-MM-dd"),
                DailyRevenue = orders
                    .GroupBy(order => order.OrderDate.Date)
                    .OrderBy(group => group.Key)
                    .Select(group => new DailyRevenueViewModel
                    {
                        Date = group.Key,
                        Revenue = group.Sum(order => order.TotalAmount),
                        Orders = group.Count()
                    })
                    .ToList(),
                TopProducts = topProducts,
                Statuses = orders
                    .GroupBy(order => order.Status)
                    .Select(group => new OrderStatusReportViewModel
                    {
                        Status = group.Key,
                        Count = group.Count()
                    })
                    .OrderByDescending(item => item.Count)
                    .ToList()
            };

            return View(model);
        }

        // 🥇 Top sản phẩm bán chạy
        public async Task<IActionResult> TopProducts()
        {
            var top = await _context.OrderDetails
                .Include(d => d.ProductVariant)
                .ThenInclude(v => v.Product)
                .GroupBy(d => d.ProductVariant.Product.Name)
                .Select(g => new
                {
                    ProductName = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Quantity * x.Price)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(10)
                .ToListAsync();

            return View(top);
        }

        // 📤 Xuất Excel
        public async Task<IActionResult> ExportExcel(DateTime? from, DateTime? to)
        {
            var fromDate = from?.Date;
            var toExclusive = to?.Date.AddDays(1);

            var query = _context.Orders
                .Where(o =>
                    OrderStatuses.RevenueStatuses.Contains(o.Status) ||
                    o.PaymentStatus == PaymentStatuses.Paid);

            if (fromDate.HasValue)
                query = query.Where(o => o.OrderDate >= fromDate.Value);

            if (toExclusive.HasValue)
                query = query.Where(o => o.OrderDate < toExclusive.Value);

            var orders = await query.ToListAsync();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("DoanhThu");

            sheet.Cell(1, 1).Value = "Mã đơn";
            sheet.Cell(1, 2).Value = "Ngày";
            sheet.Cell(1, 3).Value = "Tổng tiền";
            sheet.Cell(1, 4).Value = "Trạng thái đơn";
            sheet.Cell(1, 5).Value = "Trạng thái thanh toán";
            sheet.Cell(1, 6).Value = "Mã giảm giá";
            sheet.Cell(1, 7).Value = "Tiền giảm";
            sheet.Cell(1, 8).Value = "Phương thức";

            sheet.Range(1, 1, 1, 8).Style.Font.Bold = true;
            sheet.Range(1, 1, 1, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#E11D48");
            sheet.Range(1, 1, 1, 8).Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var o in orders)
            {
                sheet.Cell(row, 1).Value = $"ORD{o.Id:D6}";
                sheet.Cell(row, 2).Value = o.OrderDate.ToString("dd/MM/yyyy HH:mm");
                sheet.Cell(row, 3).Value = o.TotalAmount;
                sheet.Cell(row, 4).Value = o.Status;
                sheet.Cell(row, 5).Value = o.PaymentStatus;
                sheet.Cell(row, 6).Value = o.DiscountCode ?? "";
                sheet.Cell(row, 7).Value = o.DiscountAmount;
                sheet.Cell(row, 8).Value = o.PaymentMethod;
                row++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BaoCaoDoanhThu.xlsx"
            );
        }
    }
}
