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

    // 🧾 Trang Checkout
    public async Task<IActionResult> Checkout()
    {
        var userId = _userManager.GetUserId(User);

        var cart = await _context.CartItems
            .Include(c => c.ProductVariant)
            .ThenInclude(v => v.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        if (!cart.Any())
            return RedirectToAction("Index", "Cart");

        return View(cart);
    }

    // 📦 Tạo đơn hàng
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(string address, string phone)
    {
        var userId = _userManager.GetUserId(User);

        var cart = await _context.CartItems
            .Include(c => c.ProductVariant)
            .ThenInclude(v => v.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        if (!cart.Any())
            return RedirectToAction("Index", "Cart");

        // 🔹 Tạo Order mới
        var order = new Order
        {
            UserId = userId,
            ShippingAddress = address,
            PhoneNumber = phone,
            TotalAmount = cart.Sum(x => x.ProductVariant.Product.Price * x.Quantity),
            Status = "Pending",
            OrderDate = DateTime.Now
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(); // 🔹 Save trước để có OrderId

        // 🔹 Thêm OrderDetails
        foreach (var item in cart)
        {
            _context.OrderDetails.Add(new OrderDetail
            {
                OrderId = order.Id,
                ProductVariantId = item.ProductVariantId,
                Quantity = item.Quantity,
                Price = item.ProductVariant.Product.Price
                // Lưu ý: KHÔNG cần ProductId nữa, tránh xung đột FK
            });
        }

        // 🔹 Xóa giỏ hàng
        _context.CartItems.RemoveRange(cart);

        await _context.SaveChangesAsync();

        // 🔹 Chuyển sang VNPay (nếu muốn thanh toán online)
        return RedirectToAction("VNPay", "Payment", new { orderId = order.Id });
    }

    // ✅ Đặt hàng thành công
    public async Task<IActionResult> OrderSuccess(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(d => d.ProductVariant)
            .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        return View(order);
    }

    // 📜 Lịch sử đơn hàng
    public async Task<IActionResult> MyOrders()
    {
        var userId = _userManager.GetUserId(User);

        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }
}
