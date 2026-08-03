using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using WebBanHangOnline.Models.Momo;

namespace WebBanHangOnline.Services.Momo
{
    public class MomoService : IMomoService
    {
        private readonly IOptions<MomoOptionModel> _options;
        private readonly HttpClient _httpClient;

        public MomoService(IOptions<MomoOptionModel> options, HttpClient httpClient)
        {
            _options = options;
            _httpClient = httpClient;
        }

        public Task<MomoCreatePaymentResponseModel> CreatePaymentMomo(Order model)
        {
            return CreatePaymentMomo(model, null, null);
        }

        public async Task<MomoCreatePaymentResponseModel> CreatePaymentMomo(
            Order model,
            string? redirectUrl,
            string? ipnUrl)
        {
            var options = _options.Value;
            var endpoint = string.IsNullOrWhiteSpace(options.MomoApiUrl)
                ? "https://test-payment.momo.vn/v2/gateway/api/create"
                : options.MomoApiUrl;

            if (string.IsNullOrWhiteSpace(options.PartnerCode)
                || string.IsNullOrWhiteSpace(options.AccessKey)
                || string.IsNullOrWhiteSpace(options.SecretKey))
            {
                throw new InvalidOperationException("Cấu hình MoMo chưa đầy đủ. Vui lòng điền MomoAPI:PartnerCode, AccessKey và SecretKey.");
            }

            var amount = ((long)Math.Round(model.TotalAmount, MidpointRounding.AwayFromZero)).ToString();
            var requestId = $"{model.Id}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var orderId = requestId;
            var orderInfo = $"Thanh toan don hang {model.Id}";
            var extraData = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"orderId\":{model.Id}}}"));
            var requestType = string.IsNullOrWhiteSpace(options.RequestType) ? "payWithMethod" : options.RequestType;
            redirectUrl = string.IsNullOrWhiteSpace(redirectUrl) ? options.ReturnUrl : redirectUrl;
            ipnUrl = string.IsNullOrWhiteSpace(ipnUrl) ? options.NotifyUrl : ipnUrl;

            if (string.IsNullOrWhiteSpace(redirectUrl) || string.IsNullOrWhiteSpace(ipnUrl))
            {
                throw new InvalidOperationException("Cấu hình MoMo ReturnUrl/NotifyUrl chưa đầy đủ.");
            }

            var rawSignature =
                $"accessKey={options.AccessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={options.PartnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";

            var request = new MomoCreatePaymentRequest
            {
                PartnerCode = options.PartnerCode,
                PartnerName = "XuanBac Fashion",
                StoreId = "XuanBacFashion",
                RequestId = requestId,
                Amount = amount,
                OrderId = orderId,
                OrderInfo = orderInfo,
                RedirectUrl = redirectUrl,
                IpnUrl = ipnUrl,
                Lang = "vi",
                RequestType = requestType,
                AutoCapture = true,
                ExtraData = extraData,
                Signature = HmacSha256(options.SecretKey, rawSignature)
            };

            var response = await _httpClient.PostAsJsonAsync(endpoint, request);
            var result = await response.Content.ReadFromJsonAsync<MomoCreatePaymentResponseModel>();

            if (result == null)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Không đọc được phản hồi MoMo: {body}");
            }

            return result;
        }

        private static string HmacSha256(string key, string rawData)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        private sealed class MomoCreatePaymentRequest
        {
            [JsonPropertyName("partnerCode")]
            public string PartnerCode { get; set; } = string.Empty;

            [JsonPropertyName("partnerName")]
            public string PartnerName { get; set; } = string.Empty;

            [JsonPropertyName("storeId")]
            public string StoreId { get; set; } = string.Empty;

            [JsonPropertyName("requestId")]
            public string RequestId { get; set; } = string.Empty;

            [JsonPropertyName("amount")]
            public string Amount { get; set; } = string.Empty;

            [JsonPropertyName("orderId")]
            public string OrderId { get; set; } = string.Empty;

            [JsonPropertyName("orderInfo")]
            public string OrderInfo { get; set; } = string.Empty;

            [JsonPropertyName("redirectUrl")]
            public string RedirectUrl { get; set; } = string.Empty;

            [JsonPropertyName("ipnUrl")]
            public string IpnUrl { get; set; } = string.Empty;

            [JsonPropertyName("lang")]
            public string Lang { get; set; } = "vi";

            [JsonPropertyName("requestType")]
            public string RequestType { get; set; } = string.Empty;

            [JsonPropertyName("autoCapture")]
            public bool AutoCapture { get; set; }

            [JsonPropertyName("extraData")]
            public string ExtraData { get; set; } = string.Empty;

            [JsonPropertyName("signature")]
            public string Signature { get; set; } = string.Empty;
        }
    }
}
