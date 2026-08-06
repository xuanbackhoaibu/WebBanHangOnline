using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;
using WebBanHangOnline.Services;

[Authorize]
public class OrderController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrderController(ApplicationDbContext context,
                           UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // =========================================================
    // 🧾 CHECKOUT TOÀN BỘ GIỎ (Giữ nguyên như cũ)
    // =========================================================
    public async Task<IActionResult> Checkout()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var cart = await GetUserCart(user.Id);

        if (!cart.Any())
            return RedirectToAction("Index", "Cart");

        SetUserInfoToViewBag(user);
        await SetDiscountToViewBag(cart, null);

        return View(cart);
    }

    // =========================================================
    // 🧾 CHECKOUT SẢN PHẨM ĐÃ CHỌN
    // =========================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckoutSelected(List<int> selectedItems)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        if (selectedItems == null || !selectedItems.Any())
            return RedirectToAction("Index", "Cart");

        var cart = await GetUserCart(user.Id, selectedItems);

        if (!cart.Any())
            return RedirectToAction("Index", "Cart");

        SetUserInfoToViewBag(user);
        await SetDiscountToViewBag(cart, null);

        return View("Checkout", cart);
    }

    // =========================================================
// ⚡ BUY NOW - CHECKOUT TRỰC TIẾP
// =========================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BuyNow(int variantId, int quantity)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId);

        if (variant == null)
            return NotFound();

        if (quantity <= 0 || quantity > variant.Stock)
            return BadRequest("Số lượng không hợp lệ.");

        // Tạo cart tạm (KHÔNG LƯU DB)
        var fakeCart = new List<CartItem>
        {
            new CartItem
            {
                ProductVariantId = variant.Id,
                Quantity = quantity,
                ProductVariant = variant
            }
        };

        SetUserInfoToViewBag(user);
        await SetDiscountToViewBag(fakeCart, null);

        return View("Checkout", fakeCart);
    }

    [HttpGet]
    public async Task<IActionResult> PreviewDiscount(string? code, decimal subtotal)
    {
        if (subtotal < 0)
        {
            return BadRequest();
        }

        var result = await CalculateDiscount(code, subtotal);
        var finalTotal = Math.Max(0, subtotal - (result.IsValid ? result.DiscountAmount : 0));

        return Json(new
        {
            isValid = result.IsValid,
            code = result.Code,
            discountAmount = result.IsValid ? result.DiscountAmount : 0,
            finalTotal,
            message = result.IsValid
                ? $"Đã áp dụng mã {result.Code}."
                : result.Message
        });
    }

    // =========================================================
    // 📦 PLACE ORDER (XỬ LÝ CẢ 2 TRƯỜNG HỢP)
    // =========================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(
        string address,
        string phone,
        string paymentMethod,
        string? selectedDiscountCode,
        string? manualDiscountCode,
        List<int> selectedItems,
        int? variantId,
        int quantity = 1)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var cart = await GetCheckoutItems(user.Id, selectedItems, variantId, quantity);
        var isBuyNow = variantId.HasValue;

        if (!cart.Any())
            return RedirectToAction("Index", "Cart");
        
        

        // ================= VALIDATION =================

        if (string.IsNullOrWhiteSpace(paymentMethod))
            return BadRequest("Vui lòng chọn phương thức thanh toán.");

        if (!PaymentMethods.All.Contains(paymentMethod))
            return BadRequest("Phương thức thanh toán không hợp lệ.");

        address = string.IsNullOrWhiteSpace(address)
            ? user.StreetAddress
            : address;

        phone = string.IsNullOrWhiteSpace(phone)
            ? user.PhoneNumber
            : phone;

        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(phone))
            return BadRequest("Vui lòng nhập đầy đủ địa chỉ và số điện thoại.");

        // ================= UPDATE PROFILE =================

        bool needUpdate = false;

        if (user.StreetAddress != address)
        {
            user.StreetAddress = address;
            needUpdate = true;
        }

        if (user.PhoneNumber != phone)
        {
            user.PhoneNumber = phone;
            needUpdate = true;
        }

        if (needUpdate)
            await _userManager.UpdateAsync(user);

        // ================= TRANSACTION =================

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 🔹 Check tồn kho trên dữ liệu mới nhất
            foreach (var item in cart)
            {
                await _context.Entry(item.ProductVariant).ReloadAsync();

                if (!InventoryService.CanReserve(item.ProductVariant, item.Quantity))
                {
                    await transaction.RollbackAsync();
                    return BadRequest(
                        $"Sản phẩm {item.ProductVariant.Product?.Name ?? "này"} không đủ số lượng.");
                }
            }

            var discountCode = ResolveDiscountCode(selectedDiscountCode, manualDiscountCode);
            var subtotal = cart.Sum(x => GetItemPrice(x) * x.Quantity);
            var discountResult = await CalculateDiscount(discountCode, subtotal);
            if (!discountResult.IsValid)
            {
                await transaction.RollbackAsync();
                SetUserInfoToViewBag(user);
                await SetDiscountToViewBag(cart, selectedDiscountCode, manualDiscountCode);
                ViewBag.DiscountError = discountResult.Message;
                return View("Checkout", cart);
            }

            var finalTotal = Math.Max(0, subtotal - discountResult.DiscountAmount);

            // 🔹 Tạo Order
            var order = new Order
            {
                UserId = user.Id,
                ShippingAddress = address,
                PhoneNumber = phone,
                SubtotalAmount = subtotal,
                DiscountAmount = discountResult.DiscountAmount,
                DiscountCode = discountResult.Code,
                TotalAmount = finalTotal,
                Status = OrderStatuses.Pending,
                OrderDate = DateTime.Now,
                PaymentMethod = paymentMethod,
                PaymentStatus = PaymentStatuses.Unpaid
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            if (discountResult.DiscountCode != null)
            {
                discountResult.DiscountCode.UsedCount += 1;
            }

            // 🔹 Tạo OrderDetails + trừ kho
            foreach (var item in cart)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.Id,
                    ProductVariantId = item.ProductVariantId,
                    Quantity = item.Quantity,
                    Price = GetItemPrice(item)
                });

                InventoryService.Reserve(item.ProductVariant, item.Quantity);
            }

            if (!isBuyNow)
            {
                _context.CartItems.RemoveRange(cart);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // ================= REDIRECT THANH TOÁN =================

            switch (paymentMethod)
            {
                case PaymentMethods.VnPay:
                    return RedirectToAction("VNPay", "Payment",
                        new { orderId = order.Id });

                case PaymentMethods.Momo:
                    return RedirectToAction("Momo", "Payment",
                        new { orderId = order.Id });

                case PaymentMethods.VietQr:
                    return RedirectToAction("VietQR", "Payment",
                        new { orderId = order.Id });

                case PaymentMethods.Card:
                    return RedirectToAction("Card", "Payment",
                        new { orderId = order.Id });

                default: // COD
                    order.Status = OrderStatuses.Confirmed;
                    order.PaymentStatus = PaymentStatuses.Unpaid;
                    order.PaymentDate = null;
                    await _context.SaveChangesAsync();
                    return RedirectToAction("OrderSuccess",
                        new { id = order.Id });
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Đã xảy ra lỗi khi tạo đơn hàng.");
        }
    }

    // =========================================================
    // ✅ ORDER SUCCESS
    // =========================================================
    public async Task<IActionResult> OrderSuccess(int id)
    {
        var userId = _userManager.GetUserId(User);

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (order == null)
            return NotFound();

        return View(order);
    }

    // =========================================================
    // 📜 MY ORDERS
    // =========================================================
    public async Task<IActionResult> MyOrders()
    {
        var userId = _userManager.GetUserId(User);

        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.ProductVariant)
            .ThenInclude(pv => pv.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }
    
    // =========================================================
// ❌ CANCEL ORDER
// =========================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var userId = _userManager.GetUserId(User);

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(d => d.ProductVariant)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (order == null)
            return NotFound();

        if (order.Status != OrderStatuses.Pending)
            return BadRequest("Chỉ có thể hủy đơn đang chờ xác nhận.");

        // 🔄 hoàn lại stock
        foreach (var item in order.OrderDetails)
        {
            InventoryService.Release(item.ProductVariant, item.Quantity);
        }

        order.Status = OrderStatuses.Cancelled;

        await _context.SaveChangesAsync();

        return RedirectToAction("MyOrders");
    }

    // =========================================================
    // 🔧 PRIVATE METHODS (TỐI ƯU HÓA)
    // =========================================================

    private async Task<List<CartItem>> GetUserCart(
        string userId,
        List<int>? selectedItems = null)
    {
        var query = _context.CartItems
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.Product)
            .Where(c => c.UserId == userId);

        if (selectedItems != null && selectedItems.Any())
        {
            query = query.Where(c => selectedItems.Contains(c.Id));
        }

        return await query.ToListAsync();
    }

    private async Task<List<CartItem>> GetCheckoutItems(
        string userId,
        List<int>? selectedItems,
        int? variantId,
        int quantity)
    {
        if (variantId.HasValue)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == variantId.Value);

            if (variant == null || quantity <= 0)
            {
                return new List<CartItem>();
            }

            return new List<CartItem>
            {
                new CartItem
                {
                    UserId = userId,
                    ProductVariantId = variant.Id,
                    ProductVariant = variant,
                    Quantity = quantity
                }
            };
        }

        return await GetUserCart(userId, selectedItems);
    }

    private static decimal GetItemPrice(CartItem item)
    {
        return item.ProductVariant.Price > 0
            ? item.ProductVariant.Price
            : item.ProductVariant.Product.FinalPrice;
    }

    private async Task SetDiscountToViewBag(
        List<CartItem> cart,
        string? selectedDiscountCode,
        string? manualDiscountCode = null)
    {
        var subtotal = cart.Sum(x => GetItemPrice(x) * x.Quantity);
        var code = ResolveDiscountCode(selectedDiscountCode, manualDiscountCode);
        var discount = await CalculateDiscount(code, subtotal);

        ViewBag.Subtotal = subtotal;
        ViewBag.DiscountCode = code?.Trim().ToUpperInvariant();
        ViewBag.SelectedDiscountCode = string.IsNullOrWhiteSpace(manualDiscountCode)
            ? selectedDiscountCode?.Trim().ToUpperInvariant()
            : string.Empty;
        ViewBag.ManualDiscountCode = manualDiscountCode?.Trim().ToUpperInvariant();
        ViewBag.DiscountAmount = discount.IsValid ? discount.DiscountAmount : 0;
        ViewBag.FinalTotal = Math.Max(0, subtotal - (discount.IsValid ? discount.DiscountAmount : 0));
        ViewBag.PublicDiscounts = await GetPublicDiscounts(subtotal, selectedDiscountCode);
    }

    private static string? ResolveDiscountCode(string? selectedDiscountCode, string? manualDiscountCode)
    {
        return !string.IsNullOrWhiteSpace(manualDiscountCode)
            ? manualDiscountCode
            : selectedDiscountCode;
    }

    private async Task<List<CheckoutVoucherViewModel>> GetPublicDiscounts(decimal subtotal, string? selectedCode)
    {
        var now = DateTime.Now;
        var normalizedSelectedCode = selectedCode?.Trim().ToUpperInvariant();

        var codes = await _context.DiscountCodes
            .Where(code => code.IsPublic && code.IsActive)
            .Where(code => !code.StartsAt.HasValue || code.StartsAt.Value <= now)
            .Where(code => !code.EndsAt.HasValue || code.EndsAt.Value >= now)
            .Where(code => !code.UsageLimit.HasValue || code.UsedCount < code.UsageLimit.Value)
            .OrderBy(code => code.MinimumOrderAmount)
            .ThenByDescending(code => code.DiscountValue)
            .ToListAsync();

        return codes.Select(code =>
        {
            var discount = CalculateDiscountAmount(code, subtotal);
            var isAvailable = subtotal >= code.MinimumOrderAmount;

            return new CheckoutVoucherViewModel
            {
                Code = code.Code,
                Description = code.Description,
                DiscountText = GetDiscountText(code),
                ConditionText = code.MinimumOrderAmount > 0
                    ? $"Đơn từ {code.MinimumOrderAmount:N0} VND"
                    : "Áp dụng cho mọi đơn",
                DiscountAmount = isAvailable ? discount : 0,
                IsAvailable = isAvailable,
                IsSelected = code.Code == normalizedSelectedCode,
                DisabledReason = isAvailable
                    ? null
                    : $"Cần thêm {(code.MinimumOrderAmount - subtotal):N0} VND"
            };
        }).ToList();
    }

    private async Task<DiscountCalculation> CalculateDiscount(string? code, decimal subtotal)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return DiscountCalculation.Valid(null, null, 0);
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        var discountCode = await _context.DiscountCodes
            .FirstOrDefaultAsync(item => item.Code == normalizedCode);

        if (discountCode == null)
        {
            return DiscountCalculation.Invalid("Mã giảm giá không tồn tại.");
        }

        var now = DateTime.Now;
        if (!discountCode.IsActive)
        {
            return DiscountCalculation.Invalid("Mã giảm giá đã bị tắt.");
        }

        if (discountCode.StartsAt.HasValue && discountCode.StartsAt.Value > now)
        {
            return DiscountCalculation.Invalid("Mã giảm giá chưa đến thời gian sử dụng.");
        }

        if (discountCode.EndsAt.HasValue && discountCode.EndsAt.Value < now)
        {
            return DiscountCalculation.Invalid("Mã giảm giá đã hết hạn.");
        }

        if (discountCode.UsageLimit.HasValue && discountCode.UsedCount >= discountCode.UsageLimit.Value)
        {
            return DiscountCalculation.Invalid("Mã giảm giá đã hết lượt sử dụng.");
        }

        if (subtotal < discountCode.MinimumOrderAmount)
        {
            return DiscountCalculation.Invalid(
                $"Đơn hàng cần tối thiểu {discountCode.MinimumOrderAmount:N0} VND để dùng mã này.");
        }

        var amount = CalculateDiscountAmount(discountCode, subtotal);
        return DiscountCalculation.Valid(discountCode, normalizedCode, amount);
    }

    private static decimal CalculateDiscountAmount(DiscountCode discountCode, decimal subtotal)
    {
        var amount = discountCode.DiscountType == DiscountTypes.Percent
            ? subtotal * discountCode.DiscountValue / 100
            : discountCode.DiscountValue;

        if (discountCode.MaximumDiscountAmount.HasValue)
        {
            amount = Math.Min(amount, discountCode.MaximumDiscountAmount.Value);
        }

        return Math.Min(amount, subtotal);
    }

    private static string GetDiscountText(DiscountCode discountCode)
    {
        if (discountCode.DiscountType == DiscountTypes.Percent)
        {
            var maxText = discountCode.MaximumDiscountAmount.HasValue
                ? $" tối đa {discountCode.MaximumDiscountAmount.Value:N0} VND"
                : string.Empty;

            return $"Giảm {discountCode.DiscountValue:N0}%{maxText}";
        }

        return $"Giảm {discountCode.DiscountValue:N0} VND";
    }

    private void SetUserInfoToViewBag(ApplicationUser user)
    {
        ViewBag.FullName = user.FullName;
        ViewBag.Phone = user.PhoneNumber;
        ViewBag.Address = user.StreetAddress;
    }

    private sealed record DiscountCalculation(
        bool IsValid,
        DiscountCode? DiscountCode,
        string? Code,
        decimal DiscountAmount,
        string? Message)
    {
        public static DiscountCalculation Valid(DiscountCode? discountCode, string? code, decimal amount)
            => new(true, discountCode, code, amount, null);

        public static DiscountCalculation Invalid(string message)
            => new(false, null, null, 0, message);
    }
}
