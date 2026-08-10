using System.Security;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;

namespace WebBanHangOnline.Controllers;

public sealed class SitemapController : Controller
{
    private readonly ApplicationDbContext _context;

    public SitemapController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Index()
    {
        var root = $"{Request.Scheme}://{Request.Host}";
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var products = await _context.Products
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.ProductId)
            .Select(product => new
            {
                product.ProductId,
                product.Slug
            })
            .ToListAsync();

        var xml = new StringBuilder();
        xml.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        xml.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

        AppendUrl(xml, $"{root}/", today, "daily", "1.0");
        AppendUrl(xml, $"{root}/Product", today, "daily", "0.9");
        AppendUrl(xml, $"{root}/Home/About", today, "monthly", "0.7");

        foreach (var product in products)
        {
            var slug = string.IsNullOrWhiteSpace(product.Slug)
                ? "san-pham"
                : product.Slug;
            AppendUrl(xml, $"{root}/san-pham/{slug}-{product.ProductId}", today, "weekly", "0.8");
        }

        xml.AppendLine("</urlset>");

        return Content(xml.ToString(), "application/xml", Encoding.UTF8);
    }

    private static void AppendUrl(
        StringBuilder xml,
        string location,
        string lastModified,
        string changeFrequency,
        string priority)
    {
        xml.AppendLine("  <url>");
        xml.AppendLine($"    <loc>{SecurityElement.Escape(location)}</loc>");
        xml.AppendLine($"    <lastmod>{lastModified}</lastmod>");
        xml.AppendLine($"    <changefreq>{changeFrequency}</changefreq>");
        xml.AppendLine($"    <priority>{priority}</priority>");
        xml.AppendLine("  </url>");
    }
}
