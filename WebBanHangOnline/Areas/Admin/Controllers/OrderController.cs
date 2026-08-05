using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Order
        public async Task<IActionResult> Index(
            string? search,
            string? status,
            string? paymentStatus,
            DateTime? from,
            DateTime? to)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                var normalizedOrderCode = keyword
                    .TrimStart('#')
                    .Replace("ORD", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .TrimStart('0');
                var hasOrderId = int.TryParse(
                    string.IsNullOrWhiteSpace(normalizedOrderCode) ? "0" : normalizedOrderCode,
                    out var orderId);

                query = query.Where(o =>
                    (hasOrderId && o.Id == orderId) ||
                    (o.User != null && (
                        (o.User.Email ?? string.Empty).Contains(keyword) ||
                        (o.User.UserName ?? string.Empty).Contains(keyword) ||
                        (o.User.FullName ?? string.Empty).Contains(keyword))) ||
                    (o.PhoneNumber ?? string.Empty).Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                query = query.Where(o => o.PaymentStatus == paymentStatus);
            }

            var fromDate = from?.Date;
            var toExclusive = to?.Date.AddDays(1);

            if (fromDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= fromDate.Value);
            }

            if (toExclusive.HasValue)
            {
                query = query.Where(o => o.OrderDate < toExclusive.Value);
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.PaymentStatus = paymentStatus;
            ViewBag.From = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.Date.ToString("yyyy-MM-dd");
            ViewBag.OrderStatuses = OrderStatuses.AdminEditableStatuses;
            ViewBag.PaymentStatuses = new[]
            {
                PaymentStatuses.Unpaid,
                PaymentStatuses.Paid,
                PaymentStatuses.Failed,
                PaymentStatuses.Refunded
            };
            ViewBag.TotalFilteredOrders = orders.Count;
            ViewBag.TotalFilteredRevenue = orders
                .Where(o => OrderStatuses.RevenueStatuses.Contains(o.Status) || o.PaymentStatus == PaymentStatuses.Paid)
                .Sum(o => o.TotalAmount);

            return View(orders);
        }

        // GET: Admin/Order/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductVariant)
                .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: Admin/Order/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? returnUrl = null)
        {
            if (!OrderStatuses.AdminEditableStatuses.Contains(status))
            {
                TempData["ErrorMessage"] = "Trạng thái không hợp lệ";
                return RedirectAfterUpdate(id, returnUrl);
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = status;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật trạng thái thành công.";
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = "Không thể cập nhật trạng thái: " + ex.Message;
            }

            return RedirectAfterUpdate(id, returnUrl);
        }

        private IActionResult RedirectAfterUpdate(int id, string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Order/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order); // view xác nhận xóa
        }

        // POST: Admin/Order/DeleteConfirmed/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Xóa chi tiết trước
            if (order.OrderDetails != null && order.OrderDetails.Any())
            {
                _context.OrderDetails.RemoveRange(order.OrderDetails);
            }

            // Xóa đơn hàng
            _context.Orders.Remove(order);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đơn hàng đã được xóa thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
