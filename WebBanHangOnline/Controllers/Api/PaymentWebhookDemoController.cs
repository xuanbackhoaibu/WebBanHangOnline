using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Controllers.Api;

[ApiController]
[Route("api/payments/demo-webhook")]
[Authorize(Roles = "Admin")]
public class PaymentWebhookDemoController : ControllerBase
{
    private static readonly string[] AllowedStatuses =
    {
        OrderStatuses.Paid,
        OrderStatuses.Failed,
        OrderStatuses.Refunded
    };
    private readonly ApplicationDbContext _context;

    public PaymentWebhookDemoController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmPayment([FromBody] DemoPaymentWebhookRequest request)
    {
        if (!AllowedStatuses.Contains(request.Status))
        {
            return BadRequest(new
            {
                message = "Invalid status",
                allowedStatuses = AllowedStatuses
            });
        }

        var order = await _context.Orders.FirstOrDefaultAsync(item => item.Id == request.OrderId);
        if (order == null)
        {
            return NotFound(new { message = "Order not found" });
        }

        if (request.Amount != order.TotalAmount)
        {
            return BadRequest(new
            {
                message = "Invalid amount",
                expectedAmount = order.TotalAmount,
                receivedAmount = request.Amount
            });
        }

        if (OrderStatuses.IsFinalPaymentStatus(order.Status))
        {
            return Ok(new
            {
                message = "Order already finalized",
                orderId = order.Id,
                order.Status,
                order.PaymentDate
            });
        }

        order.Status = request.Status;
        order.PaymentDate = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Payment status updated",
            orderId = order.Id,
            order.Status,
            order.PaymentMethod,
            order.TotalAmount,
            order.PaymentDate,
            request.TransactionCode,
            request.Provider
        });
    }
}

public class DemoPaymentWebhookRequest
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = OrderStatuses.Paid;
    public string Provider { get; set; } = "DemoGateway";
    public string TransactionCode { get; set; } = string.Empty;
}
