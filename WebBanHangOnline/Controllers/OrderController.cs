using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;

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
        List<int> selectedItems)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var cart = await GetUserCart(user.Id, selectedItems);

        if (!cart.Any())
            return RedirectToAction("Index", "Cart");
        
        

        // ================= VALIDATION =================

        if (string.IsNullOrWhiteSpace(paymentMethod))
            return BadRequest("Vui lòng chọn phương thức thanh toán.");

        var validMethods = new[] { "COD", "VNPay", "Momo", "ZaloPay", "Card" };

        if (!validMethods.Contains(paymentMethod))
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
            // 🔹 Check tồn kho
            foreach (var item in cart)
            {
                if (item.ProductVariant.Stock < item.Quantity)
                {
                    return BadRequest(
                        $"Sản phẩm {item.ProductVariant.Product.Name} không đủ số lượng.");
                }
            }

            // 🔹 Tạo Order
            var order = new Order
            {
                UserId = user.Id,
                ShippingAddress = address,
                PhoneNumber = phone,
                TotalAmount = cart.Sum(x => x.ProductVariant.Price * x.Quantity),
                Status = "Pending",
                OrderDate = DateTime.Now,
                PaymentMethod = paymentMethod
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
                    Price = item.ProductVariant.Price
                });

                item.ProductVariant.Stock -= item.Quantity;
            }

            // 🔹 Xóa đúng sản phẩm đã đặt
            _context.CartItems.RemoveRange(cart);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // ================= REDIRECT THANH TOÁN =================

            switch (paymentMethod)
            {
                case "VNPay":
                    return RedirectToAction("VNPay", "Payment",
                        new { orderId = order.Id });

                case "Momo":
                    return RedirectToAction("Momo", "Payment",
                        new { orderId = order.Id });

                case "ZaloPay":
                    return RedirectToAction("ZaloPay", "Payment",
                        new { orderId = order.Id });

                case "Card":
                    return RedirectToAction("Card", "Payment",
                        new { orderId = order.Id });

                default: // COD
                    order.Status = "Confirmed";
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
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

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

        if (order.Status != "Pending")
            return BadRequest("Chỉ có thể hủy đơn đang chờ xác nhận.");

        // 🔄 hoàn lại stock
        foreach (var item in order.OrderDetails)
        {
            item.ProductVariant.Stock += item.Quantity;
        }

        order.Status = "Cancelled";

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

    private void SetUserInfoToViewBag(ApplicationUser user)
    {
        ViewBag.FullName = user.FullName;
        ViewBag.Phone = user.PhoneNumber;
        ViewBag.Address = user.StreetAddress;
    }
}
