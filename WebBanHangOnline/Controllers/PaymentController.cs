using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WebBanHangOnline.Data;
using WebBanHangOnline.Helpers;
using WebBanHangOnline.Models;
using WebBanHangOnline.Models.Momo;
using WebBanHangOnline.Services.Momo;

namespace WebBanHangOnline.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMomoService _momoService;
        private readonly MomoOptionModel _momoOptions;
        private readonly IWebHostEnvironment _environment;
        // ================================
        // 💳 VIETQR PAYMENT
        // ================================
        public async Task<IActionResult> VietQR(int orderId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == _userManager.GetUserId(User));

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPaid(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
                return NotFound();

            if (!PaymentStatuses.IsFinal(order.PaymentStatus))
            {
                order.PaymentStatus = PaymentStatuses.AwaitingConfirmation;
                order.PaymentDate = null;
                order.AdminNote = AppendPaymentNote(order.AdminNote, "Khách hàng báo đã chuyển khoản VietQR, chờ admin xác nhận.");

                AddPaymentTransaction(
                    order.Id,
                    "VietQR",
                    $"VQR-{order.Id}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                    order.TotalAmount,
                    PaymentStatuses.AwaitingConfirmation,
                    true,
                    "{}",
                    "Customer submitted bank transfer confirmation.");
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("OrderSuccess", "Order", new { id = orderId });
        }
        public PaymentController(
            ApplicationDbContext context,
            IConfiguration config,
            UserManager<ApplicationUser> userManager,
            IMomoService momoService,
            IOptions<MomoOptionModel> momoOptions,
            IWebHostEnvironment environment)
        {
            _context = context;
            _config = config;
            _userManager = userManager;
            _momoService = momoService;
            _momoOptions = momoOptions.Value;
            _environment = environment;
        }

        public async Task<IActionResult> Momo(int orderId)
        {
            try
            {
                var order = await GetCurrentUserOrder(orderId);
                if (order == null)
                    return NotFound("Đơn hàng không tồn tại");

                var redirectUrl = BuildAbsoluteUrl(nameof(MomoReturn));
                var ipnUrl = BuildAbsoluteUrl(nameof(MomoIpn));
                var response = await _momoService.CreatePaymentMomo(order, redirectUrl, ipnUrl);

                if (response.ResultCode != 0 && response.ErrorCode != 0)
                {
                    TempData["ErrorMessage"] = $"MoMo không tạo được giao dịch: {response.Message ?? response.LocalMessage}";
                    return RedirectToAction("MyOrders", "Order");
                }

                if (string.IsNullOrWhiteSpace(response.PayUrl))
                {
                    TempData["ErrorMessage"] = "MoMo không trả về link thanh toán.";
                    return RedirectToAction("MyOrders", "Order");
                }

                return Redirect(response.PayUrl);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("MyOrders", "Order");
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> MomoReturn()
        {
            var parameters = Request.Query
                .Where(item => !string.IsNullOrWhiteSpace(item.Key))
                .ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.OrdinalIgnoreCase);

            var signatureValid = VerifyMomoSignature(parameters);
            var order = await FindMomoOrder(parameters.TryGetValue("orderId", out var momoOrderId) ? momoOrderId : null);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng MoMo.";
                return RedirectToAction("MyOrders", "Order");
            }

            AddPaymentTransaction(
                order.Id,
                "MoMo",
                parameters.TryGetValue("transId", out var returnTransId) ? returnTransId : string.Empty,
                TryReadDecimal(parameters, "amount") ?? 0,
                parameters.TryGetValue("resultCode", out var returnCode) && returnCode == "0" ? PaymentStatuses.Paid : PaymentStatuses.Failed,
                signatureValid,
                JsonSerializer.Serialize(parameters),
                signatureValid ? "MoMo return received." : "MoMo return rejected: invalid signature.");

            if (!signatureValid)
            {
                await _context.SaveChangesAsync();
                TempData["ErrorMessage"] = "MoMo trả về chữ ký không hợp lệ.";
                return RedirectToAction("MyOrders", "Order");
            }

            if (!IsPaymentAmountValid(order, parameters))
            {
                await _context.SaveChangesAsync();
                TempData["ErrorMessage"] = "MoMo trả về số tiền không khớp đơn hàng.";
                return RedirectToAction("MyOrders", "Order");
            }

            var resultCode = Request.Query["resultCode"].ToString();
            if (!PaymentStatuses.IsFinal(order.PaymentStatus))
            {
                order.PaymentStatus = resultCode == "0" ? PaymentStatuses.Paid : PaymentStatuses.Failed;
                if (resultCode == "0" && order.Status == OrderStatuses.Pending)
                {
                    order.Status = OrderStatuses.Confirmed;
                }
                order.PaymentDate = DateTime.Now;
            }
            await _context.SaveChangesAsync();

            TempData[resultCode == "0" ? "SuccessMessage" : "ErrorMessage"] =
                resultCode == "0" ? "Thanh toán MoMo thành công." : $"Thanh toán MoMo thất bại: {Request.Query["message"]}";

            return RedirectToAction("OrderSuccess", "Order", new { id = order.Id });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> MomoIpn()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            var root = document.RootElement;
            var parameters = ReadJsonObjectAsDictionary(root);
            var signatureValid = VerifyMomoSignature(parameters);

            var order = await FindMomoOrder(root.TryGetProperty("orderId", out var orderIdElement)
                ? orderIdElement.GetString()
                : null);

            if (order == null)
                return Ok(new { resultCode = 1, message = "Order not found" });

            var paymentStatus = root.TryGetProperty("resultCode", out var statusElement) && statusElement.GetInt32() == 0
                ? PaymentStatuses.Paid
                : PaymentStatuses.Failed;

            AddPaymentTransaction(
                order.Id,
                "MoMo",
                parameters.TryGetValue("transId", out var transId) ? transId : string.Empty,
                TryReadDecimal(parameters, "amount") ?? 0,
                paymentStatus,
                signatureValid,
                body,
                signatureValid ? "MoMo IPN received." : "MoMo IPN rejected: invalid signature.");

            if (!signatureValid)
            {
                await _context.SaveChangesAsync();
                return Ok(new { resultCode = 1, message = "Invalid signature" });
            }

            if (!IsPaymentAmountValid(order, parameters))
            {
                await _context.SaveChangesAsync();
                return Ok(new { resultCode = 1, message = "Invalid amount" });
            }

            var resultCode = root.TryGetProperty("resultCode", out var resultCodeElement)
                ? resultCodeElement.GetInt32()
                : 1;

            if (!PaymentStatuses.IsFinal(order.PaymentStatus))
            {
                order.PaymentStatus = resultCode == 0 ? PaymentStatuses.Paid : PaymentStatuses.Failed;
                if (resultCode == 0 && order.Status == OrderStatuses.Pending)
                {
                    order.Status = OrderStatuses.Confirmed;
                }
                order.PaymentDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return Ok(new { resultCode = 0, message = "Confirm Success" });
        }

        public async Task<IActionResult> Card(int orderId)
        {
            if (!_environment.IsDevelopment())
            {
                return Forbid();
            }

            var order = await GetCurrentUserOrder(orderId);
            if (order == null)
                return NotFound("Đơn hàng không tồn tại");

            order.PaymentStatus = PaymentStatuses.Paid;
            if (order.Status == OrderStatuses.Pending)
            {
                order.Status = OrderStatuses.Confirmed;
            }
            order.PaymentDate = DateTime.Now;
            AddPaymentTransaction(
                order.Id,
                "CardDemo",
                $"CARD-DEMO-{order.Id}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                order.TotalAmount,
                PaymentStatuses.Paid,
                true,
                "{}",
                "Development-only demo card payment.");
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thanh toán thẻ demo thành công.";
            return RedirectToAction("OrderSuccess", "Order", new { id = order.Id });
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
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == _userManager.GetUserId(User));

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
                    string.IsNullOrEmpty(vnpUrl))
                {
                    return BadRequest("Cấu hình VNPay chưa đầy đủ");
                }
                if (string.IsNullOrWhiteSpace(returnUrl))
                {
                    returnUrl = BuildAbsoluteUrl(nameof(VnPayReturn));
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
        [AllowAnonymous]
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
                order.PaymentStatus = isSuccess ? PaymentStatuses.Paid : PaymentStatuses.Failed;
                if (isSuccess && order.Status == OrderStatuses.Pending)
                {
                    order.Status = OrderStatuses.Confirmed;
                }
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
        [AllowAnonymous]
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
                if (PaymentStatuses.IsFinal(order.PaymentStatus))
                {
                    return Ok(new { RspCode = "02", Message = "Order already confirmed" });
                }

                // Cập nhật trạng thái
                vnpParams.TryGetValue("vnp_ResponseCode", out string? responseCode);
                
                if (responseCode == "00")
                {
                    order.PaymentStatus = PaymentStatuses.Paid;
                    if (order.Status == OrderStatuses.Pending)
                    {
                        order.Status = OrderStatuses.Confirmed;
                    }
                    order.PaymentDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Ok(new { RspCode = "00", Message = "Confirm Success" });
                }
                else
                {
                    order.PaymentStatus = PaymentStatuses.Failed;
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

        private async Task<Order?> GetCurrentUserOrder(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.ProductVariant)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        }

        private string BuildAbsoluteUrl(string actionName)
        {
            return Url.Action(
                action: actionName,
                controller: "Payment",
                values: null,
                protocol: Request.Scheme,
                host: Request.Host.ToString()) ?? string.Empty;
        }

        private async Task<Order?> FindMomoOrderFromRequest()
        {
            var orderId = Request.Query["orderId"].ToString();
            return await FindMomoOrder(orderId);
        }

        private async Task<Order?> FindMomoOrder(string? momoOrderId)
        {
            if (string.IsNullOrWhiteSpace(momoOrderId))
                return null;

            var orderIdText = momoOrderId.Split('_')[0];
            return int.TryParse(orderIdText, out var orderId)
                ? await _context.Orders.FindAsync(orderId)
                : null;
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

        private bool VerifyMomoSignature(IReadOnlyDictionary<string, string> data)
        {
            if (string.IsNullOrWhiteSpace(_momoOptions.SecretKey) ||
                !data.TryGetValue("signature", out var receivedSignature) ||
                string.IsNullOrWhiteSpace(receivedSignature))
            {
                return false;
            }

            var rawData = BuildMomoResponseRawData(data);
            if (string.IsNullOrWhiteSpace(rawData))
            {
                return false;
            }

            var computedSignature = HmacSha256(_momoOptions.SecretKey, rawData);
            if (string.Equals(computedSignature, receivedSignature, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var fallbackRawData = BuildCanonicalSignatureData(data, "signature");
            var fallbackSignature = HmacSha256(_momoOptions.SecretKey, fallbackRawData);
            return string.Equals(fallbackSignature, receivedSignature, StringComparison.OrdinalIgnoreCase);
        }

        private string BuildMomoResponseRawData(IReadOnlyDictionary<string, string> data)
        {
            var orderedKeys = new[]
            {
                "accessKey",
                "amount",
                "extraData",
                "message",
                "orderId",
                "orderInfo",
                "orderType",
                "partnerCode",
                "payType",
                "requestId",
                "responseTime",
                "resultCode",
                "transId"
            };

            var values = orderedKeys
                .Where(key => data.TryGetValue(key, out var value) && value != null)
                .Select(key => $"{key}={data[key]}");

            return string.Join("&", values);
        }

        private static string BuildCanonicalSignatureData(
            IReadOnlyDictionary<string, string> data,
            params string[] excludedKeys)
        {
            var excluded = excludedKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            return string.Join("&", data
                .Where(item => !excluded.Contains(item.Key) && item.Value != null)
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => $"{item.Key}={item.Value}"));
        }

        private static string HmacSha256(string key, string input)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        private static Dictionary<string, string> ReadJsonObjectAsDictionary(JsonElement root)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (root.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

            foreach (var property in root.EnumerateObject())
            {
                result[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => property.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => string.Empty,
                    _ => property.Value.GetRawText()
                };
            }

            return result;
        }

        private static decimal? TryReadDecimal(IReadOnlyDictionary<string, string> data, string key)
        {
            return data.TryGetValue(key, out var value) && decimal.TryParse(value, out var amount)
                ? amount
                : null;
        }

        private static bool IsPaymentAmountValid(Order order, IReadOnlyDictionary<string, string> data)
        {
            var amount = TryReadDecimal(data, "amount");
            if (!amount.HasValue)
            {
                return false;
            }

            var expectedAmount = Math.Round(order.TotalAmount, 0, MidpointRounding.AwayFromZero);
            return amount.Value == expectedAmount;
        }

        private void AddPaymentTransaction(
            int orderId,
            string provider,
            string transactionCode,
            decimal amount,
            string status,
            bool isSignatureValid,
            string rawPayload,
            string note)
        {
            _context.PaymentTransactions.Add(new PaymentTransaction
            {
                OrderId = orderId,
                Provider = provider,
                TransactionCode = transactionCode,
                Amount = amount,
                Status = status,
                IsSignatureValid = isSignatureValid,
                RawPayload = rawPayload,
                Note = note
            });
        }

        private static string AppendPaymentNote(string? currentNote, string note)
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {note}";
            return string.IsNullOrWhiteSpace(currentNote)
                ? line
                : $"{currentNote}{Environment.NewLine}{line}";
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
