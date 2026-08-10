namespace WebBanHangOnline.Models;

public static class PaymentStatuses
{
    public const string Unpaid = "Unpaid";
    public const string AwaitingConfirmation = "AwaitingConfirmation";
    public const string Paid = "Paid";
    public const string Failed = "Failed";
    public const string Refunded = "Refunded";

    public static readonly string[] FinalStatuses =
    {
        Paid,
        Failed,
        Refunded
    };

    public static readonly string[] AdminEditableStatuses =
    {
        Unpaid,
        AwaitingConfirmation,
        Paid,
        Failed,
        Refunded
    };

    public static bool IsFinal(string status)
    {
        return FinalStatuses.Contains(status);
    }
}
