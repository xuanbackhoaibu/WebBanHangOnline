using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;
using WebBanHangOnline.Services;

[Authorize]
public class CartController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CartController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // =========================
    // 🛒 XEM GIỎ HÀNG
    // =========================
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        var cartItems = await _context.CartItems
            .Where(c => c.UserId == userId)
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.Product)
            .ToListAsync();

        return View(cartItems);
    }

    // =========================
    // ➕ THÊM VÀO GIỎ
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int variantId, int quantity = 1)
    {
        if (quantity <= 0)
            quantity = 1;

        var userId = _userManager.GetUserId(User);

        // ✅ 1. KIỂM TRA VARIANT TỒN TẠI (FIX FK)
        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId);

        if (variant == null)
        {
            return BadRequest("Product variant không tồn tại");
        }

        // ✅ 2. KIỂM TRA ĐÃ CÓ TRONG GIỎ CHƯA
        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.ProductVariantId == variantId);

        var requestedQuantity = (cartItem?.Quantity ?? 0) + quantity;
        if (!InventoryService.CanReserve(variant, requestedQuantity))
        {
            return BadRequest($"Sản phẩm {variant.Product?.Name ?? "này"} chỉ còn {variant.Stock} sản phẩm.");
        }

        if (cartItem == null)
        {
            cartItem = new CartItem
            {
                UserId = userId,
                ProductVariantId = variantId,
                Quantity = quantity
            };

            _context.CartItems.Add(cartItem);
        }
        else
        {
            cartItem.Quantity += quantity;
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // =========================
    // 🔄 CẬP NHẬT SỐ LƯỢNG
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, int quantity)
    {
        if (quantity <= 0)
            return RedirectToAction(nameof(Index));

        var userId = _userManager.GetUserId(User);

        var item = await _context.CartItems
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (item == null)
            return NotFound();

        if (!InventoryService.CanReserve(item.ProductVariant, quantity))
        {
            return BadRequest($"Sản phẩm {item.ProductVariant.Product?.Name ?? "này"} chỉ còn {item.ProductVariant.Stock} sản phẩm.");
        }

        item.Quantity = quantity;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // =========================
    // ❌ XÓA KHỎI GIỎ
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        var userId = _userManager.GetUserId(User);

        var item = await _context.CartItems
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (item != null)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
