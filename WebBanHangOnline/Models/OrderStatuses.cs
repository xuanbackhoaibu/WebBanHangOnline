namespace WebBanHangOnline.Models;

public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Paid = "Paid";
    public const string Shipping = "Shipping";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Failed = "Failed";
    public const string Refunded = "Refunded";

    public static readonly string[] RevenueStatuses =
    {
        Confirmed,
        Completed
    };

    public static readonly string[] AdminEditableStatuses =
    {
        Pending,
        Confirmed,
        Shipping,
        Completed,
        Cancelled
    };

    public static bool IsFinalPaymentStatus(string status)
    {
        return PaymentStatuses.IsFinal(status);
    }
}
