using Microsoft.EntityFrameworkCore; // nhớ thêm
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;
using WebBanHangOnline.Services;

namespace WebBanHangOnline.Controllers;

public class HomeController : Controller
{
    private const string HasEnteredShopSessionKey = "HasEnteredShop";
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly ICatalogCacheService _catalogCache;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context,
        ICatalogCacheService catalogCache)
    {
        _logger = logger;
        _context = context;
        _catalogCache = catalogCache;
    }

    public IActionResult Welcome()
    {
        return RedirectToAction(nameof(About));
    }

    public async Task<IActionResult> Index(int? categoryId, bool enterShop = false)
    {
        if (enterShop)
        {
            HttpContext.Session.SetString(HasEnteredShopSessionKey, "true");
        }
        else if (HttpContext.Session.GetString(HasEnteredShopSessionKey) != "true")
        {
            return RedirectToAction(nameof(About));
        }

        // Lấy danh sách danh mục để hiển thị filter
        ViewBag.Categories = await _catalogCache.GetActiveCategoriesAsync();
        ViewBag.TopSellingProducts = await _catalogCache.GetTopSellingProductsAsync();

        // Lấy danh sách sản phẩm
        var productsQuery = _context.Products
            .Include(p => p.Images)
            .Include(p => p.Category)  
            .Where(p => p.IsActive);

        if (categoryId.HasValue && categoryId > 0)
        {
            productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await productsQuery.ToListAsync();
        
        var notifications = await _context.Notifications
            .Where(n => n.IsActive)
            .OrderByDescending(n => n.Priority)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync();

        ViewBag.Notifications = notifications;


        return View(products);
    }

    public IActionResult Privacy()
    {
        return View();
    }
    
    public IActionResult About()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}



