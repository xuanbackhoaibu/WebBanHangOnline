using WebBanHangOnline.Models;

namespace WebBanHangOnline.Tests;

public class OrderStatusesTests
{
    [Theory]
    [InlineData(OrderStatuses.Paid)]
    [InlineData(OrderStatuses.Failed)]
    [InlineData(OrderStatuses.Refunded)]
    public void IsFinalPaymentStatus_ReturnsTrue_ForFinalPaymentStatuses(string status)
    {
        Assert.True(OrderStatuses.IsFinalPaymentStatus(status));
    }

    [Theory]
    [InlineData(OrderStatuses.Pending)]
    [InlineData(OrderStatuses.Confirmed)]
    [InlineData(OrderStatuses.Shipping)]
    [InlineData(OrderStatuses.Completed)]
    [InlineData(OrderStatuses.Cancelled)]
    public void IsFinalPaymentStatus_ReturnsFalse_ForOperationalStatuses(string status)
    {
        Assert.False(OrderStatuses.IsFinalPaymentStatus(status));
    }

    [Fact]
    public void AdminEditableStatuses_ContainsOnlyAllowedAdminTransitions()
    {
        Assert.DoesNotContain(OrderStatuses.Paid, OrderStatuses.AdminEditableStatuses);
        Assert.DoesNotContain(OrderStatuses.Failed, OrderStatuses.AdminEditableStatuses);
        Assert.Contains(OrderStatuses.Completed, OrderStatuses.AdminEditableStatuses);
    }
}
