using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;

namespace WebBanHangOnline.Controllers.Api;

[ApiController]
[Route("api/catalog")]
public class CatalogApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CatalogApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("products")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int? categoryId,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Variants)
            .Where(product => product.IsActive);

        if (categoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(product =>
                product.Name.Contains(normalizedKeyword) ||
                product.Description.Contains(normalizedKeyword));
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderBy(product => product.ProductId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new
            {
                id = product.ProductId,
                product.Name,
                product.Slug,
                category = product.Category == null ? null : product.Category.Name,
                price = product.Price,
                finalPrice = product.FinalPrice,
                isFlashSaleActive = product.IsFlashSaleActive,
                thumbnail = product.Thumbnail,
                stock = product.Variants.Sum(variant => variant.Stock)
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            totalItems,
            totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            items
        });
    }

    [HttpGet("products/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Images)
            .Include(item => item.Variants)
            .Where(item => item.IsActive)
            .Select(item => new
            {
                id = item.ProductId,
                item.Name,
                item.Slug,
                item.Description,
                category = item.Category == null ? null : item.Category.Name,
                price = item.Price,
                finalPrice = item.FinalPrice,
                item.Thumbnail,
                images = item.Images.Select(image => image.ImageUrl),
                variants = item.Variants.Select(variant => new
                {
                    id = variant.Id,
                    variant.Size,
                    variant.Color,
                    variant.Stock,
                    variant.Price
                }),
                averageRating = 0,
                reviewCount = 0
            })
            .FirstOrDefaultAsync(item => item.id == id);

        return product == null ? NotFound(new { message = "Product not found" }) : Ok(product);
    }

    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.CategoryId)
            .Select(category => new
            {
                id = category.CategoryId,
                category.Name,
                productCount = category.Products.Count(product => product.IsActive)
            })
            .ToListAsync();

        return Ok(categories);
    }
}
