using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WebBanHangOnline.Data;
using WebBanHangOnline.Helpers;
using WebBanHangOnline.Models;
using WebBanHangOnline.Models.Vnpay;

namespace WebBanHangOnline.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        // ================================
        // 💳 VIETQR PAYMENT
        // ================================
        public async Task<IActionResult> VietQR(int orderId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            // 👉 LẤY ĐÚNG TIỀN ĐƠN HÀNG
            decimal amount = order.TotalAmount;

            // 👉 TẠO QR
            string qrUrl = VietQrHelper.GenerateQr(
                bank: "VCB",                // đổi theo bank bạn
                account: "123456789",       // STK của bạn
                amount: amount,
                content: $"DH{order.Id}",
                name: "CUONG TRAN MANH"
            );

            ViewBag.QrUrl = qrUrl;
            ViewBag.Amount = amount;
            ViewBag.OrderId = order.Id;

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ConfirmPaid(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null)
                return NotFound();

            order.Status = "Confirmed";
            order.PaymentDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("OrderSuccess", "Order", new { id = orderId });
        }
        public PaymentController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        
        // Chuyển hướng sang VNPay
        public async Task<IActionResult> VnPay(int orderId)
        {
            // Lấy đơn hàng, bao gồm chi tiết
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound("Đơn hàng không tồn tại");
// 👉 THÊM DÒNG NÀY
Console.WriteLine("TOTAL DB: " + order.TotalAmount);
            // Lấy cấu hình VNPay Sandbox
            var hashSecret = _config["VNPay:HashSecret"]?.Trim();
            var tmnCode = _config["VNPay:TmnCode"]?.Trim();
            var vnpUrl = _config["VNPay:Url"]?.Trim();

            if (hashSecret == null || tmnCode == null || vnpUrl == null)
                return BadRequest("Cấu hình VNPay chưa đầy đủ.");

            var returnUrl = _config["VNPay:ReturnUrl"];
            if (string.IsNullOrWhiteSpace(returnUrl))
                return BadRequest("Không tạo được ReturnUrl VNPay.");

            // Thêm kiểm tra dữ liệu OrderDetails trước khi gửi sang VNPay
            foreach (var detail in order.OrderDetails)
            {
                if (detail.Price <= 0 || detail.Quantity <= 0 || detail.ProductVariantId <= 0)
                    return BadRequest("Dữ liệu OrderDetails không hợp lệ.");
            }

            // Khởi tạo tham số VNPay
            var vnpayParams = new SortedDictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", tmnCode },
                { "vnp_Amount", ((long)(order.TotalAmount * 100)).ToString() }, // VNPay tính theo đơn vị nhỏ
                { "vnp_CurrCode", "VND" },
                { "vnp_TxnRef", order.Id.ToString() },
                { "vnp_OrderInfo", $"Thanh toán đơn hàng #{order.Id}" },
                { "vnp_OrderType", "other" },
                { "vnp_Locale", "vn" },
                { "vnp_ReturnUrl", returnUrl },
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "vnp_IpAddr", "127.0.0.1" },
            };

            var queryString = BuildQueryString(vnpayParams, true);
var secureHash = HmacSha512(hashSecret, queryString);

var paymentUrl = $"{vnpUrl}?{queryString}&vnp_SecureHash={secureHash}";
Console.WriteLine(paymentUrl);

return Redirect(paymentUrl);

        }

        // Callback VNPay trả về
        public async Task<IActionResult> VnPayReturn()
        {
            var hashSecret = _config["VNPay:HashSecret"]?.Trim();
            if (hashSecret == null) return BadRequest("Cấu hình VNPay chưa đầy đủ.");

            var vnpParams = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());

            // Lấy chữ ký từ VNPay
            vnpParams.TryGetValue("vnp_SecureHash", out string? vnpSecureHash);
            vnpParams.Remove("vnp_SecureHash");
            vnpParams.Remove("vnp_SecureHashType");

            // Tính lại chữ ký theo đúng format ký của VNPay (sort + URL-encode value)
            var computedHashData = BuildQueryString(vnpParams, urlEncodeValues: true);
            var computedHash = HmacSha512(hashSecret, computedHashData);

            if (!string.Equals(computedHash, vnpSecureHash, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Sai chữ ký VNPay!";
                return RedirectToAction("MyOrders", "Order");
            }

            // Lấy order
            if (!vnpParams.TryGetValue("vnp_TxnRef", out string? orderIdStr) || !int.TryParse(orderIdStr, out int orderId))
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                return RedirectToAction("MyOrders", "Order");
            }

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Đơn hàng không tồn tại";
                return RedirectToAction("MyOrders", "Order");
            }

            vnpParams.TryGetValue("vnp_ResponseCode", out string? responseCode);

            order.Status = responseCode == "00" ? "Paid" : "Failed";
            order.PaymentDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = order.Status == "Paid"
                ? "Thanh toán thành công"
                : $"Thanh toán thất bại: {responseCode}";

            return RedirectToAction("OrderSuccess", "Order", new { id = order.Id });
        }

        private static string HmacSha512(string key, string input)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        private static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> data, bool urlEncodeValues)
        {
            var ordered = data
                .Where(kv => !string.IsNullOrEmpty(kv.Key) && kv.Value != null)
                .OrderBy(kv => kv.Key, StringComparer.Ordinal);

            return string.Join("&", ordered.Select(kv =>
            {
                var value = urlEncodeValues ? Uri.EscapeDataString(kv.Value) : kv.Value;
                return $"{kv.Key}={value}";
            }));
        }
    }
}
