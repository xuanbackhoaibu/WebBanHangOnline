namespace WebBanHangOnline.Helpers
{
    public class VietQrHelper
    {
        public static string GenerateQr(string bank, string account, decimal amount, string content, string name)
        {
            return $"https://img.vietqr.io/image/{bank}-{account}-compact.png" +
                   $"?amount={amount}&addInfo={Uri.EscapeDataString(content)}&accountName={Uri.EscapeDataString(name)}";
        }
    }
}
