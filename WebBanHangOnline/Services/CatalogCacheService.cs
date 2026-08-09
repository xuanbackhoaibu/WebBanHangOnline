using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Services;

public sealed class CatalogCacheService : ICatalogCacheService
{
    private const string ActiveCategoriesKey = "catalog:categories:active";
    private const string TopSellingProductsKeyPrefix = "catalog:products:top-selling:";
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CatalogCacheService> _logger;

    public CatalogCacheService(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<CatalogCacheService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Category>> GetActiveCategoriesAsync()
    {
        return await _cache.GetOrCreateAsync(ActiveCategoriesKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            entry.SlidingExpiration = TimeSpan.FromMinutes(10);
            _logger.LogDebug("Catalog cache miss for active categories.");

            return await _context.Categories
                .AsNoTracking()
                .Where(category => category.IsActive)
                .OrderBy(category => category.CategoryId)
                .ToListAsync();
        }) ?? [];
    }

    public async Task<IReadOnlyList<Product>> GetTopSellingProductsAsync(int take = 8)
    {
        var cacheKey = $"{TopSellingProductsKeyPrefix}{take}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            entry.SlidingExpiration = TimeSpan.FromMinutes(3);
            _logger.LogDebug("Catalog cache miss for top selling products. Take: {Take}", take);

            var productIds = await _context.OrderDetails
                .AsNoTracking()
                .Where(detail =>
                    detail.Order.Status == OrderStatuses.Completed ||
                    detail.Order.Status == OrderStatuses.Confirmed ||
                    detail.Order.PaymentStatus == PaymentStatuses.Paid)
                .GroupBy(detail => detail.ProductVariant.ProductId)
                .OrderByDescending(group => group.Sum(detail => detail.Quantity))
                .Take(take)
                .Select(group => group.Key)
                .ToListAsync();

            if (!productIds.Any())
            {
                return await _context.Products
                    .AsNoTracking()
                    .Include(product => product.Images)
                    .Where(product => product.IsActive)
                    .OrderByDescending(product => product.ProductId)
                    .Take(take)
                    .ToListAsync();
            }

            var products = await _context.Products
                .AsNoTracking()
                .Include(product => product.Images)
                .Where(product => product.IsActive && productIds.Contains(product.ProductId))
                .ToListAsync();

            return products
                .OrderBy(product => productIds.IndexOf(product.ProductId))
                .ToList();
        }) ?? [];
    }

    public void ClearCatalogCache()
    {
        _cache.Remove(ActiveCategoriesKey);
        for (var take = 1; take <= 20; take++)
        {
            _cache.Remove($"{TopSellingProductsKeyPrefix}{take}");
        }
    }
}
