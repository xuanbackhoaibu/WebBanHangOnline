using System.Text.Json.Serialization;

namespace WebBanHangOnline.Models.Momo
{
    public class MomoCreatePaymentResponseModel
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("resultCode")]
        public int ResultCode { get; set; }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("localMessage")]
        public string LocalMessage { get; set; }

        [JsonPropertyName("requestType")]
        public string RequestType { get; set; }

        [JsonPropertyName("payUrl")]
        public string PayUrl { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; }

        [JsonPropertyName("qrCodeUrl")]
        public string QrCodeUrl { get; set; }

        [JsonPropertyName("deeplink")]
        public string Deeplink { get; set; }

        [JsonPropertyName("deeplinkWebInApp")]
        public string DeeplinkWebInApp { get; set; }
    }
}
