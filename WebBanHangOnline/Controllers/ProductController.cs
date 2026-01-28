using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;

namespace WebBanHangOnline.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int PAGE_SIZE = 9;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================
        // /Product
        // ============================
        public async Task<IActionResult> Index(
            string? keyword,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            string? size,
            string? color,
            string? sort,
            int page = 1)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Images) // ⚠️ để lấy ảnh
                .Where(p => p.IsActive)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(p => p.Name.Contains(keyword));

            // Filter Category
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            // Filter Price
            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice);

            // Filter Size / Color (qua Variant)
            if (!string.IsNullOrEmpty(size))
                query = query.Where(p => p.Variants.Any(v => v.Size == size));

            if (!string.IsNullOrEmpty(color))
                query = query.Where(p => p.Variants.Any(v => v.Color == color));

            // Sort
            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "newest" => query.OrderByDescending(p => p.ProductId),
                _ => query.OrderByDescending(p => p.ProductId)
            };

            // Pagination
            var totalItems = await query.CountAsync();

            var products = await query
                .Skip((page - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .ToListAsync();

            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)PAGE_SIZE);
            ViewBag.Keyword = keyword;
            ViewBag.CategoryId = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Size = size;
            ViewBag.Color = color;
            ViewBag.Sort = sort;

            return View(products);
        }

        // ============================
        // /Product/Details/5
        // ============================
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Images) // ⚠️ gallery ảnh
                .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);

            if (product == null)
                return NotFound();

            // Sản phẩm liên quan
            ViewBag.Related = await _context.Products
                .Include(p => p.Images)
                .Where(p => p.CategoryId == product.CategoryId
                         && p.ProductId != id
                         && p.IsActive)
                .Take(4)
                .ToListAsync();

            return View(product);
        }
    }
}
