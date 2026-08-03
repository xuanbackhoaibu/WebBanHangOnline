namespace WebBanHangOnline.Models;

public static class PaymentMethods
{
    public const string Cod = "COD";
    public const string VnPay = "VNPay";
    public const string Momo = "Momo";
    public const string VietQr = "VietQR";
    public const string Card = "Card";

    public static readonly string[] All =
    {
        Cod,
        VnPay,
        Momo,
        VietQr,
        Card
    };
}
