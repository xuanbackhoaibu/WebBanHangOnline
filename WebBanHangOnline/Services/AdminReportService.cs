using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;
using WebBanHangOnline.Models.ViewModels;

namespace WebBanHangOnline.Services;

public sealed class AdminReportService
{
    private readonly ApplicationDbContext _context;

    public AdminReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReportIndexViewModel> BuildRevenueReportAsync(DateTime? from, DateTime? to, int topProductTake = 5)
    {
        var fromDate = from?.Date;
        var toDate = to?.Date;
        var toExclusive = toDate?.AddDays(1);

        var orders = await BuildRevenueQuery(fromDate, toExclusive)
            .Include(order => order.User)
            .OrderByDescending(order => order.OrderDate)
            .ToListAsync();

        var orderIds = orders.Select(order => order.Id).ToList();
        var topProducts = await BuildTopProductsAsync(orderIds, topProductTake);

        return new ReportIndexViewModel
        {
            TotalRevenue = orders.Sum(order => order.TotalAmount),
            TotalDiscount = orders.Sum(order => order.DiscountAmount),
            TotalOrders = orders.Count,
            PaidOrders = orders.Count(order => order.PaymentStatus == PaymentStatuses.Paid),
            UnpaidOrders = orders.Count(order => order.PaymentStatus == PaymentStatuses.Unpaid),
            AverageOrderValue = orders.Any() ? orders.Average(order => order.TotalAmount) : 0,
            From = fromDate?.ToString("yyyy-MM-dd"),
            To = toDate?.ToString("yyyy-MM-dd"),
            FromDate = fromDate,
            ToDate = toDate,
            Orders = orders.Select(ToReportOrderRow).ToList(),
            DailyRevenue = orders
                .GroupBy(order => order.OrderDate.Date)
                .OrderBy(group => group.Key)
                .Select(group => new DailyRevenueViewModel
                {
                    Date = group.Key,
                    Revenue = group.Sum(order => order.TotalAmount),
                    Orders = group.Count()
                })
                .ToList(),
            TopProducts = topProducts,
            Statuses = orders
                .GroupBy(order => order.Status)
                .Select(group => new OrderStatusReportViewModel
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .OrderByDescending(item => item.Count)
                .ToList()
        };
    }

    public async Task<List<TopProductReportViewModel>> BuildTopProductsAsync(DateTime? from, DateTime? to, int take = 10)
    {
        var fromDate = from?.Date;
        var toExclusive = to?.Date.AddDays(1);
        var orderIds = await BuildRevenueQuery(fromDate, toExclusive)
            .Select(order => order.Id)
            .ToListAsync();

        return await BuildTopProductsAsync(orderIds, take);
    }

    public async Task<List<ReportOrderRowViewModel>> BuildDrillDownOrdersAsync(
        DateTime? date,
        string? status,
        string? paymentStatus,
        int? productId,
        string? category,
        DateTime? from,
        DateTime? to)
    {
        var fromDate = date?.Date ?? from?.Date;
        var toExclusive = date?.Date.AddDays(1) ?? to?.Date.AddDays(1);

        IQueryable<Order> query = BuildRevenueQuery(fromDate, toExclusive)
            .Include(order => order.User)
            .Include(order => order.OrderDetails)
                .ThenInclude(detail => detail.ProductVariant)
                    .ThenInclude(variant => variant.Product)
                        .ThenInclude(product => product.Category);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(order => order.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            query = query.Where(order => order.PaymentStatus == paymentStatus);
        }

        if (productId.HasValue)
        {
            query = query.Where(order => order.OrderDetails.Any(detail => detail.ProductVariant.ProductId == productId.Value));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(order => order.OrderDetails.Any(detail =>
                detail.ProductVariant.Product.Category != null &&
                detail.ProductVariant.Product.Category.Name == category));
        }

        var orders = await query
            .OrderByDescending(order => order.OrderDate)
            .ToListAsync();

        return orders.Select(ToReportOrderRow).ToList();
    }

    private IQueryable<Order> BuildRevenueQuery(DateTime? fromDate, DateTime? toExclusive)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Where(order =>
                OrderStatuses.RevenueStatuses.Contains(order.Status) ||
                order.PaymentStatus == PaymentStatuses.Paid);

        if (fromDate.HasValue)
        {
            query = query.Where(order => order.OrderDate >= fromDate.Value);
        }

        if (toExclusive.HasValue)
        {
            query = query.Where(order => order.OrderDate < toExclusive.Value);
        }

        return query;
    }

    private async Task<List<TopProductReportViewModel>> BuildTopProductsAsync(IReadOnlyCollection<int> orderIds, int take)
    {
        if (!orderIds.Any())
        {
            return new List<TopProductReportViewModel>();
        }

        return await _context.OrderDetails
            .AsNoTracking()
            .Include(detail => detail.ProductVariant)
            .ThenInclude(variant => variant.Product)
            .Where(detail => orderIds.Contains(detail.OrderId))
            .GroupBy(detail => new
            {
                detail.ProductVariant.ProductId,
                detail.ProductVariant.Product.Name
            })
            .Select(group => new TopProductReportViewModel
            {
                ProductId = group.Key.ProductId,
                ProductName = group.Key.Name,
                Quantity = group.Sum(item => item.Quantity),
                Revenue = group.Sum(item => item.Quantity * item.Price)
            })
            .OrderByDescending(item => item.Revenue)
            .Take(take)
            .ToListAsync();
    }

    private static ReportOrderRowViewModel ToReportOrderRow(Order order)
    {
        return new ReportOrderRowViewModel
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            CustomerName = order.User?.FullName ?? order.User?.UserName ?? "Khach vang lai",
            CustomerEmail = order.User?.Email ?? string.Empty,
            TotalAmount = order.TotalAmount,
            DiscountAmount = order.DiscountAmount,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            PaymentMethod = order.PaymentMethod,
            DiscountCode = order.DiscountCode
        };
    }
}
