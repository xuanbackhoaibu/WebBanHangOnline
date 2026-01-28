using Microsoft.EntityFrameworkCore; // nhớ thêm
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index(int? categoryId)
    {
        // Lấy danh sách danh mục để hiển thị filter
        ViewBag.Categories = await _context.Categories
            .Where(c => c.IsActive)
            .ToListAsync();

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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}


