using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;
using WebBanHangOnline.Services;

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
            ViewBag.PaymentStatuses = PaymentStatuses.AdminEditableStatuses;
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
                .Include(o => o.StatusHistories)
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

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            if (order.Status == OrderStatuses.Cancelled && status != OrderStatuses.Cancelled)
            {
                TempData["ErrorMessage"] = "Đơn đã hủy không thể chuyển sang trạng thái khác để tránh sai tồn kho.";
                return RedirectAfterUpdate(id, returnUrl);
            }

            if (status == OrderStatuses.Cancelled &&
                order.Status != OrderStatuses.Cancelled &&
                !CanCancelOrder(order.Status))
            {
                TempData["ErrorMessage"] = "Chỉ có thể hủy đơn đang chờ hoặc đã xác nhận.";
                return RedirectAfterUpdate(id, returnUrl);
            }

            var previousStatus = order.Status;

            if (status == OrderStatuses.Cancelled && order.Status != OrderStatuses.Cancelled)
            {
                RestoreOrderStock(order);
            }

            order.Status = status;

            if (previousStatus != status)
            {
                AddOrderHistory(order, "OrderStatus", previousStatus, status, "Cập nhật trạng thái đơn hàng.");
                AddOrderAuditLog(
                    order.Id,
                    "Status",
                    previousStatus,
                    status,
                    "Admin cập nhật trạng thái đơn hàng.");
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentStatus(int id, string paymentStatus, string? returnUrl = null)
        {
            if (!PaymentStatuses.AdminEditableStatuses.Contains(paymentStatus))
            {
                TempData["ErrorMessage"] = "Trạng thái thanh toán không hợp lệ.";
                return RedirectAfterUpdate(id, returnUrl);
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            var previousPaymentStatus = order.PaymentStatus;
            order.PaymentStatus = paymentStatus;
            order.PaymentDate = paymentStatus == PaymentStatuses.Paid ? DateTime.Now : null;

            if (paymentStatus == PaymentStatuses.Paid && order.Status == OrderStatuses.Pending)
            {
                AddOrderHistory(order, "OrderStatus", order.Status, OrderStatuses.Confirmed, "Tự xác nhận đơn sau khi ghi nhận đã thanh toán.");
                order.Status = OrderStatuses.Confirmed;
            }

            if (previousPaymentStatus != paymentStatus)
            {
                AddOrderHistory(order, "PaymentStatus", previousPaymentStatus, paymentStatus, "Cập nhật trạng thái thanh toán.");
                AddOrderAuditLog(
                    order.Id,
                    "PaymentStatus",
                    previousPaymentStatus,
                    paymentStatus,
                    "Admin cập nhật trạng thái thanh toán.");
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thanh toán thành công.";
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = "Không thể cập nhật thanh toán: " + ex.Message;
            }

            return RedirectAfterUpdate(id, returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAdminNote(int id, string? adminNote, string? returnUrl = null)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            var cleanNote = (adminNote ?? string.Empty).Trim();
            if (cleanNote.Length > 500)
            {
                cleanNote = cleanNote[..500];
            }

            if ((order.AdminNote ?? string.Empty) != cleanNote)
            {
                var previousNote = order.AdminNote ?? string.Empty;
                order.AdminNote = cleanNote;
                AddOrderHistory(order, "AdminNote", null, null, string.IsNullOrWhiteSpace(cleanNote)
                    ? "Đã xóa ghi chú nội bộ."
                    : "Đã cập nhật ghi chú nội bộ.");
                AddOrderAuditLog(
                    order.Id,
                    "AdminNote",
                    previousNote,
                    cleanNote,
                    "Admin cập nhật ghi chú nội bộ.");
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã lưu ghi chú nội bộ.";

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

        private static bool CanCancelOrder(string status)
        {
            return status == OrderStatuses.Pending || status == OrderStatuses.Confirmed;
        }

        private static void RestoreOrderStock(Order order)
        {
            foreach (var item in order.OrderDetails)
            {
                if (item.ProductVariant != null)
                {
                    InventoryService.Release(item.ProductVariant, item.Quantity);
                }
            }
        }

        private void AddOrderHistory(Order order, string changeType, string? fromValue, string? toValue, string? note)
        {
            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                ChangeType = changeType,
                FromValue = fromValue,
                ToValue = toValue,
                Note = note,
                ChangedBy = User.Identity?.Name ?? "Admin",
                ChangedAt = DateTime.Now
            });
        }

        private void AddOrderAuditLog(int orderId, string fieldName, string? oldValue, string? newValue, string description)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system",
                UserName = User.Identity?.Name ?? "Admin",
                Roles = User.IsInRole("Admin") ? "Admin" : string.Empty,
                Action = "Modified",
                EntityName = nameof(Order),
                EntityId = orderId.ToString(),
                OldValues = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string?>
                {
                    [fieldName] = oldValue,
                    ["Description"] = description
                }),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string?>
                {
                    [fieldName] = newValue,
                    ["Description"] = description
                }),
                CreatedAt = DateTime.Now
            });
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
