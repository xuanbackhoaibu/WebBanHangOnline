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

        return View("Checkout", fakeCart);
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
                    return BadRequest(
                        $"Sản phẩm {item.ProductVariant.Product?.Name ?? "này"} không đủ số lượng.");
                }
            }

            // 🔹 Tạo Order
            var order = new Order
            {
                UserId = user.Id,
                ShippingAddress = address,
                PhoneNumber = phone,
                TotalAmount = cart.Sum(x => GetItemPrice(x) * x.Quantity),
                Status = OrderStatuses.Pending,
                OrderDate = DateTime.Now,
                PaymentMethod = paymentMethod,
                PaymentStatus = PaymentStatuses.Unpaid
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

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
                    order.PaymentStatus = PaymentStatuses.Paid;
                    order.PaymentDate = DateTime.Now;
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

    private void SetUserInfoToViewBag(ApplicationUser user)
    {
        ViewBag.FullName = user.FullName;
        ViewBag.Phone = user.PhoneNumber;
        ViewBag.Address = user.StreetAddress;
    }
}
