using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
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

        // GET: Admin/Product/Template
        public IActionResult DownloadTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Products");

            // Header
            worksheet.Cell(1, 1).Value = "Name";
            worksheet.Cell(1, 2).Value = "CategoryId";
            worksheet.Cell(1, 3).Value = "Price";
            worksheet.Cell(1, 4).Value = "Description";
            worksheet.Cell(1, 5).Value = "IsActive";

            // Ví dụ mẫu (có thể xóa khi dùng thật)
            worksheet.Cell(2, 1).Value = "Áo thun trắng";
            worksheet.Cell(2, 2).Value = "1";
            worksheet.Cell(2, 3).Value = "199000";
            worksheet.Cell(2, 4).Value = "Áo thun cotton 100%";
            worksheet.Cell(2, 5).Value = "true";

            worksheet.Cell(3, 1).Value = "Quần jeans xanh";
            worksheet.Cell(3, 2).Value = "2";
            worksheet.Cell(3, 3).Value = "399000";
            worksheet.Cell(3, 4).Value = "Quần jeans co giãn";
            worksheet.Cell(3, 5).Value = "1";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"ProductImportTemplate_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return File(stream.ToArray(), contentType, fileName);
        }

        // GET: Admin/Product/Import
        public IActionResult Import()
        {
            return View();
        }

        // POST: Admin/Product/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Vui lòng chọn file Excel.");
                return View();
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
            {
                ModelState.AddModelError(string.Empty, "Chỉ hỗ trợ file Excel (.xlsx, .xls).");
                return View();
            }

            var importedProducts = new List<Product>();
            var errors = new List<string>();

            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);

                // Giả định dòng 1 là header
                var row = 2;
                while (true)
                {
                    var name = worksheet.Cell(row, 1).GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        break;
                    }

                    var categoryIdCell = worksheet.Cell(row, 2).GetString();
                    var priceCell = worksheet.Cell(row, 3).GetString();
                    var description = worksheet.Cell(row, 4).GetString() ?? string.Empty;
                    var isActiveCell = worksheet.Cell(row, 5).GetString();

                    if (!int.TryParse(categoryIdCell, out var categoryId))
                    {
                        errors.Add($"Dòng {row}: CategoryId không hợp lệ.");
                        row++;
                        continue;
                    }

                    if (!await _context.Categories.AnyAsync(c => c.CategoryId == categoryId && c.IsActive))
                    {
                        errors.Add($"Dòng {row}: Danh mục (CategoryId={categoryId}) không tồn tại hoặc không hoạt động.");
                        row++;
                        continue;
                    }

                    if (!decimal.TryParse(priceCell, out var price) || price <= 0)
                    {
                        errors.Add($"Dòng {row}: Giá không hợp lệ.");
                        row++;
                        continue;
                    }

                    var isActive = true;
                    if (!string.IsNullOrWhiteSpace(isActiveCell))
                    {
                        isActive = isActiveCell.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
                                   || isActiveCell.Trim().Equals("1");
                    }

                    var product = new Product
                    {
                        Name = name,
                        CategoryId = categoryId,
                        Description = description,
                        Price = price,
                        IsActive = isActive
                    };

                    product.GenerateSlug();

                    // Bỏ qua nếu trùng slug với sản phẩm hiện có
                    var slugExists = await _context.Products.AnyAsync(p => p.Slug == product.Slug);
                    if (slugExists)
                    {
                        errors.Add($"Dòng {row}: Sản phẩm với tên '{name}' đã tồn tại (trùng Slug).");
                        row++;
                        continue;
                    }

                    importedProducts.Add(product);
                    row++;
                }

                if (importedProducts.Count > 0)
                {
                    await _context.Products.AddRangeAsync(importedProducts);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã import thành công {importedProducts.Count} sản phẩm.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không có sản phẩm hợp lệ nào được import.";
                }

                if (errors.Count > 0)
                {
                    TempData["ImportErrors"] = string.Join("<br/>", errors);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi đọc file Excel: {ex.Message}";
                return View();
            }

            return RedirectToAction(nameof(Index));
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
