using Microsoft.Extensions.Options;
using WebBanHangOnline.Models.Momo;
namespace WebBanHangOnline.Services.Momo
{
    public class MomoService : IMomoService
    {
        private readonly IOptions<MomoOptionModel> _options;
        public MomoService(IOptions<MomoOptionModel> options)
        {
            _options = options;
        }
        public Task<MomoCreatePaymentResponseModel> CreatePaymentMomo(Order model)
        {
            throw new NotImplementedException("MoMo payment creation is not implemented yet.");
        }
    }
}
