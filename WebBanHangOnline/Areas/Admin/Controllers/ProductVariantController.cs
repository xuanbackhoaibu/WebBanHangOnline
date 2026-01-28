using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductVariantController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductVariantController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/ProductVariant/Index?productId=5
        public async Task<IActionResult> Index(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Admin/ProductVariant/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int productId,
            string size,
            string color,
            int stock)
        {
            if (string.IsNullOrWhiteSpace(size) || string.IsNullOrWhiteSpace(color))
                return RedirectToAction(nameof(Index), new { productId });

            var variant = new ProductVariant
            {
                ProductId = productId,
                Size = size,
                Color = color,
                Stock = stock
            };

            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { productId });
        }
    }
}