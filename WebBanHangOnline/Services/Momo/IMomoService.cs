using WebBanHangOnline.Models.Momo;

namespace WebBanHangOnline.Services.Momo
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentMomo(Order model);
        Task<MomoCreatePaymentResponseModel> CreatePaymentMomo(Order model, string? redirectUrl, string? ipnUrl);
    }
}
