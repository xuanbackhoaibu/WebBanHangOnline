using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WebBanHangOnline.Data;
using WebBanHangOnline.Helpers;
using WebBanHangOnline.Models;

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

        /// <summary>
        /// Tạo URL thanh toán VNPay
        /// </summary>
        public async Task<IActionResult> VnPay(int orderId)
        {
            try
            {
                // Lấy đơn hàng kèm chi tiết
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.ProductVariant)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return NotFound("Đơn hàng không tồn tại");
                }

                // Log số tiền để kiểm tra
                Console.WriteLine($"TOTAL DB: {order.TotalAmount}");
                Console.WriteLine($"Amount after *100: {(long)(order.TotalAmount * 100)}");

                // Lấy cấu hình VNPay từ appsettings.json
                var tmnCode = _config["VNPay:TmnCode"];
                var hashSecret = _config["VNPay:HashSecret"];
                var vnpUrl = _config["VNPay:Url"];
                var returnUrl = _config["VNPay:ReturnUrl"];

                // Kiểm tra cấu hình
                if (string.IsNullOrEmpty(tmnCode) || string.IsNullOrEmpty(hashSecret) || 
                    string.IsNullOrEmpty(vnpUrl) || string.IsNullOrEmpty(returnUrl))
                {
                    return BadRequest("Cấu hình VNPay chưa đầy đủ");
                }

                // Tạo mã giao dịch duy nhất
                string txnRef = $"{order.Id}_{DateTime.Now.Ticks}";

                // Tạo tham số VNPay - Sắp xếp theo thứ tự A-Z
                var vnpayParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
                {
                    { "vnp_Amount", ((long)(order.TotalAmount * 100)).ToString() },
                    { "vnp_Command", "pay" },
                    { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                    { "vnp_CurrCode", "VND" },
                    { "vnp_IpAddr", GetClientIpAddress() },
                    { "vnp_Locale", "vn" },
                    { "vnp_OrderInfo", $"Thanh toan don hang {order.Id}" },
                    { "vnp_OrderType", "other" },
                    { "vnp_ReturnUrl", returnUrl },
                    { "vnp_TmnCode", tmnCode },
                    { "vnp_TxnRef", txnRef },
                    { "vnp_Version", "2.1.0" }
                };

                // Tạo chữ ký
                var rawData = BuildRawDataString(vnpayParams);
                var secureHash = HmacSha512(hashSecret, rawData);
                var encodedQueryString = BuildEncodedQueryString(vnpayParams);
                var paymentUrl = $"{vnpUrl}?{encodedQueryString}&vnp_SecureHash={secureHash}";

                // Log debug
                Console.WriteLine("=== VNPAY DEBUG ===");
                Console.WriteLine($"Raw Data: {rawData}");
                Console.WriteLine($"Secure Hash: {secureHash}");
                Console.WriteLine($"Payment URL: {paymentUrl}");

                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi VNPay: {ex.Message}");
                return BadRequest($"Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// URL callback nhận kết quả từ VNPay
        /// </summary>
        public async Task<IActionResult> VnPayReturn()
        {
            try
            {
                var hashSecret = _config["VNPay:HashSecret"];
                if (string.IsNullOrEmpty(hashSecret))
                {
                    return BadRequest("Cấu hình HashSecret không hợp lệ");
                }

                // Lấy tất cả tham số từ query string
                var vnpParams = Request.Query
                    .Where(k => !string.IsNullOrEmpty(k.Key) && !string.IsNullOrEmpty(k.Value))
                    .ToDictionary(k => k.Key, v => v.Value.ToString());

                // Log debug
                Console.WriteLine("=== VNPAY RETURN DEBUG ===");
                foreach (var param in vnpParams)
                {
                    Console.WriteLine($"{param.Key}: {param.Value}");
                }

                // Lấy chữ ký từ VNPay
                if (!vnpParams.TryGetValue("vnp_SecureHash", out string? vnpSecureHash))
                {
                    TempData["ErrorMessage"] = "Không tìm thấy chữ ký VNPay";
                    return RedirectToAction("MyOrders", "Order");
                }

                // Xóa các tham số không dùng để tính chữ ký
                vnpParams.Remove("vnp_SecureHash");
                vnpParams.Remove("vnp_SecureHashType");

                // Kiểm tra chữ ký
                var rawData = BuildRawDataString(vnpParams);
                var computedHash = HmacSha512(hashSecret, rawData);

                Console.WriteLine($"Raw Data Verify: {rawData}");
                Console.WriteLine($"Computed Hash: {computedHash}");
                Console.WriteLine($"Received Hash: {vnpSecureHash}");

                if (!string.Equals(computedHash, vnpSecureHash, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "Chữ ký không hợp lệ";
                    return RedirectToAction("MyOrders", "Order");
                }

                // Lấy thông tin đơn hàng
                if (!vnpParams.TryGetValue("vnp_TxnRef", out string? txnRef) || string.IsNullOrEmpty(txnRef))
                {
                    TempData["ErrorMessage"] = "Không tìm thấy mã đơn hàng";
                    return RedirectToAction("MyOrders", "Order");
                }

                // Tách OrderId từ TxnRef
                string orderIdStr = txnRef.Split('_')[0];
                if (!int.TryParse(orderIdStr, out int orderId))
                {
                    TempData["ErrorMessage"] = "Mã đơn hàng không hợp lệ";
                    return RedirectToAction("MyOrders", "Order");
                }

                // Lấy đơn hàng từ database
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("MyOrders", "Order");
                }

                // Lấy thông tin từ VNPay
                vnpParams.TryGetValue("vnp_ResponseCode", out string? responseCode);
                vnpParams.TryGetValue("vnp_TransactionNo", out string? transactionNo);
                vnpParams.TryGetValue("vnp_BankCode", out string? bankCode);
                vnpParams.TryGetValue("vnp_PayDate", out string? payDate);

                // Cập nhật trạng thái đơn hàng
                bool isSuccess = responseCode == "00";
                order.Status = isSuccess ? "Paid" : "Failed";
                order.PaymentDate = DateTime.Now;

                // KHÔNG dùng Notes vì model chưa có trường này
                await _context.SaveChangesAsync();

                // Hiển thị thông báo
                if (isSuccess)
                {
                    TempData["SuccessMessage"] = $"Thanh toán thành công! Mã GD: {transactionNo}";
                }
                else
                {
                    string errorMessage = GetVnPayErrorMessage(responseCode);
                    TempData["ErrorMessage"] = $"Thanh toán thất bại: {errorMessage}";
                }

                return RedirectToAction("OrderSuccess", "Order", new { id = order.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi xử lý VNPay return: {ex.Message}");
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("MyOrders", "Order");
            }
        }

        /// <summary>
        /// Xử lý IPN từ VNPay (cập nhật tự động)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VnPayIpn()
        {
            try
            {
                var hashSecret = _config["VNPay:HashSecret"];
                if (string.IsNullOrEmpty(hashSecret))
                {
                    return Ok(new { RspCode = "97", Message = "Invalid Secret" });
                }

                // Lấy tất cả tham số từ query string
                var vnpParams = Request.Query
                    .Where(k => !string.IsNullOrEmpty(k.Key) && !string.IsNullOrEmpty(k.Value))
                    .ToDictionary(k => k.Key, v => v.Value.ToString());

                // Lấy chữ ký từ VNPay
                if (!vnpParams.TryGetValue("vnp_SecureHash", out string? vnpSecureHash))
                {
                    return Ok(new { RspCode = "97", Message = "No Secure Hash" });
                }

                // Xóa các tham số không dùng để tính chữ ký
                vnpParams.Remove("vnp_SecureHash");
                vnpParams.Remove("vnp_SecureHashType");

                // Kiểm tra chữ ký
                var rawData = BuildRawDataString(vnpParams);
                var computedHash = HmacSha512(hashSecret, rawData);

                if (!string.Equals(computedHash, vnpSecureHash, StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new { RspCode = "97", Message = "Invalid Signature" });
                }

                // Lấy thông tin đơn hàng
                if (!vnpParams.TryGetValue("vnp_TxnRef", out string? txnRef) || string.IsNullOrEmpty(txnRef))
                {
                    return Ok(new { RspCode = "01", Message = "Order not found" });
                }

                string orderIdStr = txnRef.Split('_')[0];
                if (!int.TryParse(orderIdStr, out int orderId))
                {
                    return Ok(new { RspCode = "01", Message = "Order not found" });
                }

                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return Ok(new { RspCode = "01", Message = "Order not found" });
                }

                // Kiểm tra số tiền
                vnpParams.TryGetValue("vnp_Amount", out string? amountStr);
                if (!long.TryParse(amountStr, out long amount) || amount != (long)(order.TotalAmount * 100))
                {
                    return Ok(new { RspCode = "04", Message = "Invalid amount" });
                }

                // Kiểm tra trạng thái đơn hàng (tránh cập nhật trùng)
                if (order.Status == "Paid" || order.Status == "Failed")
                {
                    return Ok(new { RspCode = "02", Message = "Order already confirmed" });
                }

                // Cập nhật trạng thái
                vnpParams.TryGetValue("vnp_ResponseCode", out string? responseCode);
                
                if (responseCode == "00")
                {
                    order.Status = "Paid";
                    order.PaymentDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Ok(new { RspCode = "00", Message = "Confirm Success" });
                }
                else
                {
                    order.Status = "Failed";
                    order.PaymentDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Ok(new { RspCode = "02", Message = "Payment failed" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IPN Error: {ex.Message}");
                return Ok(new { RspCode = "99", Message = "Unknown error" });
            }
        }

        #region Private Methods

        private string BuildRawDataString(IDictionary<string, string> data)
        {
            var orderedData = data
                .Where(kv => !string.IsNullOrEmpty(kv.Key) && kv.Value != null)
                .OrderBy(kv => kv.Key, StringComparer.Ordinal);

            return string.Join("&", orderedData.Select(kv => $"{kv.Key}={kv.Value}"));
        }

        private string BuildEncodedQueryString(IDictionary<string, string> data)
        {
            var orderedData = data
                .Where(kv => !string.IsNullOrEmpty(kv.Key) && kv.Value != null)
                .OrderBy(kv => kv.Key, StringComparer.Ordinal);

            return string.Join("&", orderedData.Select(kv => 
                $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        }

        private string HmacSha512(string key, string input)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        private string GetClientIpAddress()
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1")
                {
                    ipAddress = "127.0.0.1";
                }
                return ipAddress;
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        private string GetVnPayErrorMessage(string? responseCode)
        {
            return responseCode switch
            {
                "00" => "Giao dịch thành công",
                "01" => "Giao dịch đã tồn tại",
                "02" => "Merchant không hợp lệ",
                "03" => "Dữ liệu không đúng định dạng",
                "04" => "Khởi tạo giao dịch thất bại",
                "05" => "Giao dịch không thành công",
                "06" => "Đơn hàng đã được cập nhật",
                "07" => "Trùng dữ liệu",
                "09" => "Giao dịch bị nghi ngờ gian lận",
                "10" => "Giao dịch chờ xử lý",
                "11" => "Giao dịch đã được hoàn tiền",
                "12" => "Giao dịch đã được hoàn tiền toàn phần",
                "13" => "Giao dịch hết hạn",
                "24" => "Khách hàng hủy giao dịch",
                "51" => "Tài khoản không đủ số dư",
                "65" => "Vượt hạn mức giao dịch",
                "75" => "Ngân hàng bảo trì",
                "79" => "Sai mật khẩu thanh toán",
                "99" => "Lỗi không xác định",
                _ => "Lỗi không xác định"
            };
        }

        #endregion
    }
}