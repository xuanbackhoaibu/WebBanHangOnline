using Microsoft.Extensions.Options;
using WebBanHangOnline.Models.Momo;
using WebBanHangOnline.Models.Order;
namespace WebBanHangOnline.Services.Momo
{
    public class MomoService : IMomoService
    {
        private readonly IOptions<MomoOptionModel> _options;
        public MomoService(IOptions<MomoOptionModel> options)
        {
            _options = options;
        }
        public async Task<MomoCreatePaymentResponseModel> CreatePaymentMomo(OrderInfoModel model)
        {

        }
    }
}
