using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var items = await _context.WishlistItems
                .Where(item => item.UserId == userId)
                .Include(item => item.Product)
                    .ThenInclude(product => product!.Category)
                .Include(item => item.Product)
                    .ThenInclude(product => product!.Images)
                .OrderByDescending(item => item.CreatedAt)
                .ToListAsync();

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int productId, string? returnUrl = null)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var productExists = await _context.Products.AnyAsync(product => product.ProductId == productId && product.IsActive);
            if (!productExists)
            {
                return NotFound();
            }

            var existing = await _context.WishlistItems
                .FirstOrDefaultAsync(item => item.UserId == userId && item.ProductId == productId);

            if (existing == null)
            {
                _context.WishlistItems.Add(new WishlistItem
                {
                    UserId = userId,
                    ProductId = productId,
                    CreatedAt = DateTime.Now
                });
                TempData["WishlistMessage"] = "Đã lưu sản phẩm vào yêu thích.";
            }
            else
            {
                _context.WishlistItems.Remove(existing);
                TempData["WishlistMessage"] = "Đã bỏ sản phẩm khỏi yêu thích.";
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(wishlistItem => wishlistItem.WishlistItemId == id && wishlistItem.UserId == userId);

            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
                TempData["WishlistMessage"] = "Đã xóa sản phẩm khỏi danh sách yêu thích.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
