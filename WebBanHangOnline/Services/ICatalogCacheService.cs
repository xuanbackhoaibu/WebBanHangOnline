using WebBanHangOnline.Models;

namespace WebBanHangOnline.Services;

public interface ICatalogCacheService
{
    Task<IReadOnlyList<Category>> GetActiveCategoriesAsync();
    Task<IReadOnlyList<Product>> GetTopSellingProductsAsync(int take = 8);
    void ClearCatalogCache();
}
