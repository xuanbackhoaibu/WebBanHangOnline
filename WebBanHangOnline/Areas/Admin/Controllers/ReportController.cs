using ClosedXML.Excel;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanHangOnline.Models.ViewModels;
using WebBanHangOnline.Services;

namespace WebBanHangOnline.Areas.Admin.Controllers

{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly AdminReportService _reportService;
        private readonly AdminReportPdfRenderer _pdfRenderer;

        public ReportController(AdminReportService reportService, AdminReportPdfRenderer pdfRenderer)
        {
            _reportService = reportService;
            _pdfRenderer = pdfRenderer;
        }

        // 📊 Trang báo cáo
        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            return View(await _reportService.BuildRevenueReportAsync(from, to));
        }

        // 🥇 Top sản phẩm bán chạy
        public async Task<IActionResult> TopProducts(DateTime? from, DateTime? to)
        {
            ViewBag.From = from?.Date.ToString("yyyy-MM-dd");
            ViewBag.To = to?.Date.ToString("yyyy-MM-dd");
            return View(await _reportService.BuildTopProductsAsync(from, to));
        }

        public async Task<IActionResult> Details(
            DateTime? date,
            string? status,
            string? paymentStatus,
            int? productId,
            string? category,
            DateTime? from,
            DateTime? to)
        {
            ViewBag.Date = date?.Date.ToString("yyyy-MM-dd");
            ViewBag.Status = status;
            ViewBag.PaymentStatus = paymentStatus;
            ViewBag.ProductId = productId;
            ViewBag.Category = category;
            ViewBag.From = from?.Date.ToString("yyyy-MM-dd");
            ViewBag.To = to?.Date.ToString("yyyy-MM-dd");

            var orders = await _reportService.BuildDrillDownOrdersAsync(date, status, paymentStatus, productId, category, from, to);
            return View(orders);
        }

        // 📤 Xuất Excel
        public async Task<IActionResult> ExportExcel(DateTime? from, DateTime? to)
        {
            var report = await _reportService.BuildRevenueReportAsync(from, to, topProductTake: 10);

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
            foreach (var o in report.Orders)
            {
                sheet.Cell(row, 1).Value = o.OrderCode;
                sheet.Cell(row, 2).Value = o.OrderDate.ToString("dd/MM/yyyy HH:mm");
                sheet.Cell(row, 3).Value = o.TotalAmount;
                sheet.Cell(row, 4).Value = o.Status;
                sheet.Cell(row, 5).Value = o.PaymentStatus;
                sheet.Cell(row, 6).Value = o.DiscountCode ?? "";
                sheet.Cell(row, 7).Value = o.DiscountAmount;
                sheet.Cell(row, 8).Value = o.PaymentMethod;
                row++;
            }

            sheet.Cell(row + 1, 2).Value = "Tổng doanh thu";
            sheet.Cell(row + 1, 3).Value = report.TotalRevenue;
            sheet.Cell(row + 2, 2).Value = "Tổng giảm giá";
            sheet.Cell(row + 2, 3).Value = report.TotalDiscount;
            sheet.Range(row + 1, 2, row + 2, 3).Style.Font.Bold = true;
            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMddHHmm}.xlsx"
            );
        }

        public async Task<IActionResult> ExportPdf(DateTime? from, DateTime? to)
        {
            var report = await _reportService.BuildRevenueReportAsync(from, to, topProductTake: 10);
            var bytes = _pdfRenderer.RenderRevenueReport(report);

            return File(
                bytes,
                "application/pdf",
                $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMddHHmm}.pdf");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendScheduledNow()
        {
            BackgroundJob.Enqueue<OrderMaintenanceJobs>(job => job.SendDailyRevenueReportAsync());
            TempData["ReportMessage"] = "Đã đưa job gửi báo cáo doanh thu vào Hangfire queue.";
            return RedirectToAction(nameof(Index));
        }
    }
}
