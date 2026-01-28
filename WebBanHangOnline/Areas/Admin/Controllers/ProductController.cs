using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Admin/Product
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                                         .Include(p => p.Category)
                                         .ToListAsync();
            return View(products);
        }

        // GET: Admin/Product/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                                        .Include(p => p.Category)
                                        .Include(p => p.Images)
                                        .Include(p => p.Variants)
                                        .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) return NotFound();

            return View(product);
        }

        // GET: Admin/Product/Create
        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories
                                         .Where(c => c.IsActive)
                                         .ToList();
            return View();
        }

        // POST: Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Product product,
            IFormFile? ThumbnailFile,
            List<IFormFile>? ImageFiles)
        {
            // ✅ BẮT BUỘC – sinh Slug
            product.GenerateSlug();

            // 🔥 QUAN TRỌNG – clear lỗi ModelState của Slug
            ModelState.Remove(nameof(Product.Slug));

            if (!ModelState.IsValid || product.CategoryId == 0)
            {
                if (product.CategoryId == 0)
                    ModelState.AddModelError("CategoryId", "Vui lòng chọn danh mục");

                ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
                return View(product);
            }

            // 🔥 CHECK SLUG TRÙNG
            var slugExists = await _context.Products
                .AnyAsync(p => p.Slug == product.Slug);

            if (slugExists)
            {
                ModelState.AddModelError("Name", "Tên sản phẩm đã tồn tại (trùng Slug)");
                ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
                return View(product);
            }

            // Upload thumbnail
            if (ThumbnailFile != null)
            {
                var thumbName = await SaveFileAsync(ThumbnailFile);
                product.Thumbnail = "/images/products/" + thumbName;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Upload multiple images
            if (ImageFiles != null && ImageFiles.Count > 0)
            {
                foreach (var file in ImageFiles)
                {
                    var fileName = await SaveFileAsync(file);
                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.ProductId,
                        ImageUrl = "/images/products/" + fileName
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Product/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                                        .Include(p => p.Images)
                                        .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) return NotFound();

            ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
            return View(product);
        }

        // POST: Admin/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Product model,
            IFormFile? ThumbnailFile,
            List<IFormFile>? ImageFiles,
            int[]? DeleteImageIds)
        {
            if (id != model.ProductId) return BadRequest();

            var product = await _context.Products
                                        .Include(p => p.Images)
                                        .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) return NotFound();

            // ✅ BẮT BUỘC – cập nhật Slug
            model.GenerateSlug();

            // 🔥 QUAN TRỌNG – clear lỗi ModelState của Slug
            ModelState.Remove(nameof(Product.Slug));

            if (!ModelState.IsValid || model.CategoryId == 0)
            {
                if (model.CategoryId == 0)
                    ModelState.AddModelError("CategoryId", "Vui lòng chọn danh mục");

                ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
                return View(model);
            }

            // 🔥 CHECK SLUG TRÙNG (trừ chính nó)
            var slugExists = await _context.Products
                .AnyAsync(p => p.Slug == model.Slug && p.ProductId != id);

            if (slugExists)
            {
                ModelState.AddModelError("Name", "Tên sản phẩm đã tồn tại (trùng Slug)");
                ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
                return View(model);
            }

            // Update fields
            product.Name = model.Name;
            product.Slug = model.Slug;
            product.Description = model.Description;
            product.Price = model.Price;
            product.CategoryId = model.CategoryId;
            product.IsActive = model.IsActive;

            // Update thumbnail
            if (ThumbnailFile != null)
            {
                if (!string.IsNullOrEmpty(product.Thumbnail) && product.Thumbnail != "/images/no-image.png")
                    DeleteFile(product.Thumbnail);

                var thumbName = await SaveFileAsync(ThumbnailFile);
                product.Thumbnail = "/images/products/" + thumbName;
            }

            // Delete selected images
            if (DeleteImageIds != null && DeleteImageIds.Length > 0)
            {
                var imagesToDelete = product.Images
                                            .Where(img => DeleteImageIds.Contains(img.Id))
                                            .ToList();

                foreach (var img in imagesToDelete)
                {
                    DeleteFile(img.ImageUrl);
                    _context.ProductImages.Remove(img);
                }
            }

            // Upload new images
            if (ImageFiles != null && ImageFiles.Count > 0)
            {
                foreach (var file in ImageFiles)
                {
                    var fileName = await SaveFileAsync(file);
                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.ProductId,
                        ImageUrl = "/images/products/" + fileName
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Product/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                                        .Include(p => p.Images)
                                        .Include(p => p.Variants)
                                        .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) return NotFound();

            // Delete thumbnail
            if (!string.IsNullOrEmpty(product.Thumbnail) && product.Thumbnail != "/images/no-image.png")
                DeleteFile(product.Thumbnail);

            // Delete images
            foreach (var img in product.Images)
                DeleteFile(img.ImageUrl);

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ====================
        // Helpers
        // ====================
        private async Task<string> SaveFileAsync(IFormFile file)
        {
            var folder = Path.Combine(_env.WebRootPath, "images", "products");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(folder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fileName;
        }

        private void DeleteFile(string relativePath)
        {
            var filePath = Path.Combine(_env.WebRootPath,
                relativePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }
    }
}
