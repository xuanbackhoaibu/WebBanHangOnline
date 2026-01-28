using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;

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
            var query = _context.Orders
                .Where(o => o.Status == "Paid" || o.Status == "Completed");

            if (from.HasValue)
                query = query.Where(o => o.OrderDate >= from.Value);

            if (to.HasValue)
                query = query.Where(o => o.OrderDate <= to.Value);

            var orders = await query.ToListAsync();

            ViewBag.TotalRevenue = orders.Sum(o => o.TotalAmount);
            ViewBag.TotalOrders = orders.Count;

            return View();
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
            var query = _context.Orders
                .Where(o => o.Status == "Paid" || o.Status == "Completed");

            if (from.HasValue)
                query = query.Where(o => o.OrderDate >= from.Value);

            if (to.HasValue)
                query = query.Where(o => o.OrderDate <= to.Value);

            var orders = await query.ToListAsync();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("DoanhThu");

            sheet.Cell(1, 1).Value = "Mã đơn";
            sheet.Cell(1, 2).Value = "Ngày";
            sheet.Cell(1, 3).Value = "Tổng tiền";
            sheet.Cell(1, 4).Value = "Trạng thái";

            int row = 2;
            foreach (var o in orders)
            {
                sheet.Cell(row, 1).Value = o.Id;
                sheet.Cell(row, 2).Value = o.OrderDate.ToString("dd/MM/yyyy");
                sheet.Cell(row, 3).Value = o.TotalAmount;
                sheet.Cell(row, 4).Value = o.Status;
                row++;
            }

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
