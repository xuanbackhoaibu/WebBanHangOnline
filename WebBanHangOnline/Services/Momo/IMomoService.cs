using WebBanHangOnline.Models.Momo;

namespace WebBanHangOnline.Services.Momo
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentMomo(Order model);
    }
}
