using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Services;

public sealed class OrderMaintenanceJobs
{
    private readonly ApplicationDbContext _context;
    private readonly AdminReportService _reportService;
    private readonly IEmailSender _emailSender;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderMaintenanceJobs> _logger;

    public OrderMaintenanceJobs(
        ApplicationDbContext context,
        AdminReportService reportService,
        IEmailSender emailSender,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<OrderMaintenanceJobs> logger)
    {
        _context = context;
        _reportService = reportService;
        _emailSender = emailSender;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task CancelExpiredUnpaidOrdersAsync()
    {
        var expiredBefore = DateTime.Now.AddMinutes(-30);
        var orders = await _context.Orders
            .Include(order => order.OrderDetails)
                .ThenInclude(detail => detail.ProductVariant)
            .Where(order =>
                order.Status == OrderStatuses.Pending &&
                order.PaymentStatus == PaymentStatuses.Unpaid &&
                order.OrderDate <= expiredBefore)
            .ToListAsync();

        foreach (var order in orders)
        {
            foreach (var detail in order.OrderDetails)
            {
                InventoryService.Release(detail.ProductVariant, detail.Quantity);
            }

            order.Status = OrderStatuses.Cancelled;
            order.AdminNote = AppendSystemNote(order.AdminNote, "Auto-cancelled because payment was not completed within 30 minutes.");
        }

        try
        {
            var affectedRows = await _context.SaveChangesAsync();
            _logger.LogInformation("Expired unpaid order cancellation completed. Orders: {OrderCount}, Rows: {Rows}",
                orders.Count,
                affectedRows);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(exception, "Concurrency conflict while cancelling expired unpaid orders. Hangfire will retry this job.");
            throw;
        }
    }

    public async Task SendDailyRevenueReportAsync()
    {
        var reportDate = DateTime.Today.AddDays(-1);

        var report = await _reportService.BuildRevenueReportAsync(reportDate, reportDate, topProductTake: 5);

        var recipient = await ResolveReportRecipientAsync();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning("Daily revenue report skipped because no admin email is configured.");
            return;
        }

        var subject = $"Bao cao doanh thu ngay {reportDate:dd/MM/yyyy}";
        var body = $"""
            <h2>Bao cao doanh thu {reportDate:dd/MM/yyyy}</h2>
            <p>Tong don hop le: {report.TotalOrders}</p>
            <p>Doanh thu: {report.TotalRevenue:N0} VND</p>
            <p>Gia tri trung binh: {report.AverageOrderValue:N0} VND</p>
            <p>Da giam gia: {report.TotalDiscount:N0} VND</p>
            <h3>Top san pham</h3>
            <ul>
                {string.Join("", report.TopProducts.Select(product => $"<li>{product.ProductName}: {product.Quantity} sp - {product.Revenue:N0} VND</li>"))}
            </ul>
            """;

        await _emailSender.SendEmailAsync(recipient, subject, body);
        _logger.LogInformation("Daily revenue report sent. Date: {ReportDate}, Recipient: {Recipient}, Revenue: {Revenue}",
            reportDate,
            recipient,
            report.TotalRevenue);
    }

    public async Task DisableExpiredDiscountCodesAsync()
    {
        var now = DateTime.Now;
        var expiredCodes = await _context.DiscountCodes
            .Where(code => code.IsActive && code.EndsAt.HasValue && code.EndsAt.Value < now)
            .ToListAsync();

        foreach (var code in expiredCodes)
        {
            code.IsActive = false;
        }

        try
        {
            var affectedRows = await _context.SaveChangesAsync();
            _logger.LogInformation("Expired discount codes disabled. Codes: {CodeCount}, Rows: {Rows}",
                expiredCodes.Count,
                affectedRows);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(exception, "Concurrency conflict while disabling expired discount codes. Hangfire will retry this job.");
            throw;
        }
    }

    public async Task SyncPaymentStatusAsync(
        int orderId,
        string status,
        decimal amount,
        string provider,
        string transactionCode)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(item => item.Id == orderId);
        if (order == null)
        {
            _logger.LogWarning("Payment webhook sync skipped because order was not found. OrderId: {OrderId}", orderId);
            return;
        }

        if (amount != order.TotalAmount)
        {
            _logger.LogWarning("Payment webhook sync rejected because amount mismatched. OrderId: {OrderId}, Expected: {Expected}, Received: {Received}",
                orderId,
                order.TotalAmount,
                amount);
            return;
        }

        if (PaymentStatuses.IsFinal(order.PaymentStatus))
        {
            _logger.LogInformation("Payment webhook sync ignored because order already has final payment status. OrderId: {OrderId}, Status: {PaymentStatus}",
                orderId,
                order.PaymentStatus);
            return;
        }

        order.PaymentStatus = status;
        order.PaymentDate = DateTime.Now;
        order.AdminNote = AppendSystemNote(order.AdminNote, $"Payment webhook from {provider}: {status} ({transactionCode}).");

        if (status == PaymentStatuses.Paid && order.Status == OrderStatuses.Pending)
        {
            order.Status = OrderStatuses.Confirmed;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Payment webhook synced. OrderId: {OrderId}, Provider: {Provider}, TransactionCode: {TransactionCode}, PaymentStatus: {PaymentStatus}",
            orderId,
            provider,
            transactionCode,
            status);
    }

    private async Task<string?> ResolveReportRecipientAsync()
    {
        var configuredEmail = _configuration["Admin:ReportEmail"];
        if (!string.IsNullOrWhiteSpace(configuredEmail))
        {
            return configuredEmail;
        }

        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        return admins.FirstOrDefault(user => !string.IsNullOrWhiteSpace(user.Email))?.Email;
    }

    private static string AppendSystemNote(string? currentNote, string note)
    {
        var systemNote = $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {note}";
        return string.IsNullOrWhiteSpace(currentNote)
            ? systemNote
            : $"{currentNote}{Environment.NewLine}{systemNote}";
    }
}
