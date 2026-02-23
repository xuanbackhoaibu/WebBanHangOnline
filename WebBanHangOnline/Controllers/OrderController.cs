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

    // ============================
    // 🧾 CHECKOUT
    // ============================
    public async Task<IActionResult> Checkout()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var cart = await _context.CartItems
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.Product)
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        if (!cart.Any())
            return RedirectToAction("Index", "Cart");

        ViewBag.FullName = user.FullName;
        ViewBag.Phone = user.PhoneNumber;
        ViewBag.Address = user.StreetAddress;

        return View(cart);
    }

    // ============================
    // 📦 PLACE ORDER
    // ============================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(string address,
                                                string phone,
                                                string paymentMethod)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var cart = await _context.CartItems
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.Product)
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        if (!cart.Any())
            return RedirectToAction("Index", "Cart");

        // ===== Validate dữ liệu =====
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

// ===== CẬP NHẬT LẠI PROFILE NẾU NGƯỜI DÙNG SỬA =====
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
        {
            await _userManager.UpdateAsync(user);
        }

        // ===== Transaction để tránh lỗi =====
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 🔹 Check tồn kho
            foreach (var item in cart)
            {
                if (item.ProductVariant.Stock < item.Quantity)
                {
                    return BadRequest($"Sản phẩm {item.ProductVariant.Product.Name} không đủ số lượng.");
                }
            }

            // 🔹 Tạo Order
            var order = new Order
            {
                UserId = user.Id,
                ShippingAddress = address,
                PhoneNumber = phone,
                TotalAmount = cart.Sum(x => x.ProductVariant.Product.Price * x.Quantity),
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
                    Price = item.ProductVariant.Product.Price
                });

                item.ProductVariant.Stock -= item.Quantity;
            }

            // 🔹 Xóa giỏ hàng
            _context.CartItems.RemoveRange(cart);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // ===== Redirect thanh toán =====
            switch (paymentMethod)
            {
                case "VNPay":
                    return RedirectToAction("VNPay", "Payment", new { orderId = order.Id });

                case "Momo":
                    return RedirectToAction("Momo", "Payment", new { orderId = order.Id });

                case "ZaloPay":
                    return RedirectToAction("ZaloPay", "Payment", new { orderId = order.Id });

                case "Card":
                    return RedirectToAction("Card", "Payment", new { orderId = order.Id });

                default: // COD
                    order.Status = "Confirmed";
                    order.PaymentDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return RedirectToAction("OrderSuccess", new { id = order.Id });
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Đã xảy ra lỗi khi tạo đơn hàng.");
        }
    }

    // ============================
    // ✅ ORDER SUCCESS
    // ============================
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

    // ============================
    // 📜 MY ORDERS
    // ============================
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