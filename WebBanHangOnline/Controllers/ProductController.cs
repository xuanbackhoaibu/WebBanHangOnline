using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;

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
            bool sale = false,
            string? sort = null,
            int page = 1)
        {
            var displayKeyword = keyword?.Trim();
            var hasActiveFilter =
                !string.IsNullOrWhiteSpace(displayKeyword) ||
                categoryId.HasValue ||
                minPrice.HasValue ||
                maxPrice.HasValue ||
                !string.IsNullOrWhiteSpace(size) ||
                !string.IsNullOrWhiteSpace(color) ||
                sale ||
                !string.IsNullOrWhiteSpace(sort);

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Images) // ⚠️ để lấy ảnh
                .Include(p => p.Reviews)
                .Where(p => p.IsActive)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(displayKeyword))
            {
                var searchKeyword = displayKeyword.ToLower();

                // ===== ĐỒ NAM =====
                if (searchKeyword.Contains("nam"))
                {
                    query = query.Where(p =>
                        EF.Functions.Like(p.Category.Name, "%Đồ Nam%"));
                }
                // ===== ĐỒ NỮ =====
                else if (searchKeyword.Contains("nữ") || searchKeyword.Contains("nu"))
                {
                    query = query.Where(p =>
                        EF.Functions.Like(p.Category.Name, "%Đồ Nữ%"));
                }
                // ===== BÉ TRAI =====
                else if (searchKeyword.Contains("bé trai") || searchKeyword.Contains("be trai") || searchKeyword.Contains("trai"))
                {
                    query = query.Where(p =>
                        EF.Functions.Like(p.Category.Name, "%Bé trai%"));
                }
                // ===== BÉ GÁI =====
                else if (searchKeyword.Contains("bé gái") || searchKeyword.Contains("be gai") || searchKeyword.Contains("gái") || searchKeyword.Contains("gai"))
                {
                    query = query.Where(p =>
                        EF.Functions.Like(p.Category.Name, "%Bé Gái%"));
                }
                // ===== TÌM THEO TÊN SẢN PHẨM =====
                else
                {
                    query = query.Where(p =>
                        EF.Functions.Like(p.Name, $"%{searchKeyword}%"));
                }
            }
            

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

            if (sale)
            {
                query = query.Where(p =>
                    p.FlashSalePrice.HasValue &&
                    p.FlashSaleStart.HasValue &&
                    p.FlashSaleEnd.HasValue &&
                    DateTime.Now >= p.FlashSaleStart.Value &&
                    DateTime.Now <= p.FlashSaleEnd.Value);
            }

            // Sort
            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "rating" => query.OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(review => review.Rating) : 0),
                "stock" => query.OrderByDescending(p => p.Variants.Sum(variant => variant.Stock)),
                "newest" => query.OrderByDescending(p => p.ProductId),
                _ => query.OrderByDescending(p => p.ProductId)
            };

            // Pagination
            var totalItems = await query.CountAsync();

            var products = await query
                .Skip((page - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .ToListAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.WishlistProductIds = string.IsNullOrWhiteSpace(userId)
                ? new HashSet<int>()
                : await _context.WishlistItems
                    .Where(item => item.UserId == userId)
                    .Select(item => item.ProductId)
                    .ToHashSetAsync();

            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .ToListAsync();

            ViewBag.Colors = await _context.ProductVariants
                .Where(variant => variant.Product.IsActive && variant.Color != "")
                .Select(variant => variant.Color)
                .Distinct()
                .OrderBy(colorName => colorName)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)PAGE_SIZE);
            ViewBag.TotalItems = totalItems;
            ViewBag.HasActiveFilter = hasActiveFilter;
            ViewBag.Keyword = displayKeyword;
            ViewBag.CategoryId = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Size = size;
            ViewBag.Color = color;
            ViewBag.Sale = sale;
            ViewBag.Sort = sort;

            return View(products);
        }

        public IActionResult Sale()
        {
            return RedirectToAction(nameof(Index), new { sale = true, sort = "newest" });
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
                .Include(p => p.Reviews)
                    .ThenInclude(review => review.User)
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Review? userReview = null;
            var hasPurchased = false;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                userReview = await _context.Reviews
                    .FirstOrDefaultAsync(review => review.ProductId == id && review.UserId == userId);
                hasPurchased = await HasUserPurchasedProductAsync(userId, id);
            }

            ViewBag.UserReview = userReview;
            ViewBag.CanReview = !string.IsNullOrWhiteSpace(userId) && hasPurchased && userReview == null;
            ViewBag.ReviewGateMessage = GetReviewGateMessage(userId, hasPurchased, userReview != null);
            ViewBag.IsWishlisted = !string.IsNullOrWhiteSpace(userId)
                && await _context.WishlistItems.AnyAsync(item => item.UserId == userId && item.ProductId == id);

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, int rating, string? comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.ProductId == productId && item.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            if (!await HasUserPurchasedProductAsync(userId, productId))
            {
                TempData["ReviewError"] = "Bạn chỉ có thể đánh giá sản phẩm đã mua thành công.";
                return RedirectToAction(nameof(Details), new { id = productId, slug = product.Slug });
            }

            if (await _context.Reviews.AnyAsync(review => review.ProductId == productId && review.UserId == userId))
            {
                TempData["ReviewError"] = "Bạn đã đánh giá sản phẩm này. Hãy dùng phần sửa đánh giá của bạn.";
                return RedirectToAction(nameof(Details), new { id = productId, slug = product.Slug });
            }

            rating = Math.Clamp(rating, 1, 5);
            var cleanComment = comment?.Trim();

            if (string.IsNullOrWhiteSpace(cleanComment))
            {
                TempData["ReviewError"] = "Vui lòng nhập nội dung đánh giá.";
                return RedirectToAction(nameof(Details), new { id = productId, slug = product.Slug });
            }

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId);
            var userName = !string.IsNullOrWhiteSpace(user?.FullName)
                ? user.FullName
                : User.Identity?.Name ?? "Khách hàng";

            _context.Reviews.Add(new Review
            {
                ProductId = productId,
                UserId = userId,
                UserName = userName,
                Rating = rating,
                Comment = cleanComment.Length > 500 ? cleanComment[..500] : cleanComment,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["ReviewSuccess"] = "Cảm ơn bạn đã đánh giá sản phẩm.";

            return RedirectToAction(nameof(Details), new { id = productId, slug = product.Slug });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReview(int reviewId, int rating, string? comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var review = await _context.Reviews
                .Include(item => item.Product)
                .FirstOrDefaultAsync(item => item.ReviewId == reviewId && item.UserId == userId);

            if (review == null || review.Product == null)
            {
                return NotFound();
            }

            var cleanComment = comment?.Trim();
            if (string.IsNullOrWhiteSpace(cleanComment))
            {
                TempData["ReviewError"] = "Vui lòng nhập nội dung đánh giá.";
                return RedirectToAction(nameof(Details), new { id = review.ProductId, slug = review.Product.Slug });
            }

            review.Rating = Math.Clamp(rating, 1, 5);
            review.Comment = cleanComment.Length > 500 ? cleanComment[..500] : cleanComment;
            review.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["ReviewSuccess"] = "Đánh giá của bạn đã được cập nhật.";

            return RedirectToAction(nameof(Details), new { id = review.ProductId, slug = review.Product.Slug });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var review = await _context.Reviews
                .Include(item => item.Product)
                .FirstOrDefaultAsync(item => item.ReviewId == reviewId && item.UserId == userId);

            if (review == null || review.Product == null)
            {
                return NotFound();
            }

            var productId = review.ProductId;
            var slug = review.Product.Slug;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            TempData["ReviewSuccess"] = "Đánh giá của bạn đã được xoá.";

            return RedirectToAction(nameof(Details), new { id = productId, slug });
        }

        private async Task<bool> HasUserPurchasedProductAsync(string userId, int productId)
        {
            return await _context.Orders
                .AnyAsync(order =>
                    order.UserId == userId &&
                    (OrderStatuses.RevenueStatuses.Contains(order.Status) ||
                        order.PaymentStatus == PaymentStatuses.Paid) &&
                    order.OrderDetails.Any(detail => detail.ProductVariant.ProductId == productId));
        }

        private static string GetReviewGateMessage(string? userId, bool hasPurchased, bool hasReviewed)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return "Đăng nhập tài khoản đã mua hàng để gửi đánh giá.";
            }

            if (hasReviewed)
            {
                return "Bạn đã đánh giá sản phẩm này. Có thể sửa hoặc xoá đánh giá của mình bên dưới.";
            }

            if (!hasPurchased)
            {
                return "Chỉ tài khoản đã mua sản phẩm này mới có thể gửi đánh giá.";
            }

            return "Bạn có thể gửi đánh giá cho sản phẩm đã mua.";
        }
    }
}
