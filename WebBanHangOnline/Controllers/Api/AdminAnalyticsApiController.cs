using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Controllers.Api;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = "Admin")]
public class AdminAnalyticsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminAnalyticsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSummary()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var revenueQuery = _context.Orders
            .AsNoTracking()
            .Where(order =>
                OrderStatuses.RevenueStatuses.Contains(order.Status) ||
                order.PaymentStatus == PaymentStatuses.Paid);

        var summary = new
        {
            totalOrders = await _context.Orders.CountAsync(),
            totalRevenue = await revenueQuery.SumAsync(order => (decimal?)order.TotalAmount) ?? 0,
            todayOrders = await _context.Orders.CountAsync(order => order.OrderDate >= today && order.OrderDate < tomorrow),
            todayRevenue = await revenueQuery
                .Where(order => order.OrderDate >= today && order.OrderDate < tomorrow)
                .SumAsync(order => (decimal?)order.TotalAmount) ?? 0,
            monthRevenue = await revenueQuery
                .Where(order => order.OrderDate >= monthStart)
                .SumAsync(order => (decimal?)order.TotalAmount) ?? 0,
            totalCustomers = await _context.Users.CountAsync(),
            totalProducts = await _context.Products.CountAsync(product => product.IsActive),
            lowStockVariants = await _context.ProductVariants.CountAsync(variant => variant.Stock <= 5)
        };

        return Ok(summary);
    }

    [HttpGet("revenue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRevenue([FromQuery] int days = 7)
    {
        days = Math.Clamp(days, 1, 31);
        var from = DateTime.Today.AddDays(-(days - 1));

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(order => order.OrderDate >= from)
            .Select(order => new
            {
                date = order.OrderDate.Date,
                order.Status,
                order.PaymentStatus,
                order.TotalAmount
            })
            .ToListAsync();

        var data = Enumerable.Range(0, days)
            .Select(index => from.AddDays(index))
            .Select(date => new
            {
                date = date.ToString("yyyy-MM-dd"),
                orderCount = orders.Count(order => order.date == date),
                paidOrderCount = orders.Count(order => order.date == date &&
                    (OrderStatuses.RevenueStatuses.Contains(order.Status) || order.PaymentStatus == PaymentStatuses.Paid)),
                revenue = orders
                    .Where(order => order.date == date &&
                        (OrderStatuses.RevenueStatuses.Contains(order.Status) || order.PaymentStatus == PaymentStatuses.Paid))
                    .Sum(order => order.TotalAmount)
            });

        return Ok(data);
    }

    [HttpGet("top-products")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTopProducts([FromQuery] int take = 10)
    {
        take = Math.Clamp(take, 1, 20);

        var topProducts = await _context.OrderDetails
            .AsNoTracking()
            .Include(detail => detail.ProductVariant)
            .ThenInclude(variant => variant.Product)
            .GroupBy(detail => new
            {
                detail.ProductVariant.ProductId,
                detail.ProductVariant.Product.Name
            })
            .Select(group => new
            {
                productId = group.Key.ProductId,
                productName = group.Key.Name,
                quantity = group.Sum(item => item.Quantity),
                revenue = group.Sum(item => item.Price * item.Quantity)
            })
            .OrderByDescending(item => item.quantity)
            .Take(take)
            .ToListAsync();

        return Ok(topProducts);
    }
}
