using Microsoft.AspNetCore.Mvc;
using Google.GenAI;
using System.Text;
using System.Text.Json;

namespace WebBanHangOnline.Controllers
{
    public class ChatBotController : Controller
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http = new HttpClient();

        public ChatBotController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] ChatRequest req)
        {
            var msg = req.message.ToLower();

            if (msg.Contains("ship"))
                return Json(new { reply = "Shop miễn phí ship cho đơn trên 300k nhé!" });

            if (msg.Contains("giờ"))
                return Json(new { reply = "Shop mở cửa từ 8h đến 22h mỗi ngày!" });

            string reply = await AskGemini(req.message);

            return Json(new { reply });
        }

        async Task<string> AskGemini(string question)
        {

            string apiKey = _config["Gemini:ApiKey"];

            var body = new
            {
                contents = new[]
    {
        new {
            parts = new[] {
                new {
                    text = @"Bạn là trợ lý tư vấn của website bán quần áo XuanBac.
Nhiệm vụ:
- Tư vấn sản phẩm phù hợp cho khách
- Trả lời ngắn gọn, dễ hiểu
- Ưu tiên gợi ý mua hàng
- Dùng tiếng Việt thân thiện
Nếu khách hỏi ngoài lĩnh vực mua sắm thì trả lời lịch sự nhưng đưa về chủ đề sản phẩm.

Câu hỏi khách hàng: " + question
                }
            }
        }
    }
};



            var json = JsonSerializer.Serialize(body);

            var response = await _http.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={apiKey}",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            var result = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(result);

            if (!doc.RootElement.TryGetProperty("candidates", out var candidates))
                return "AI đang bận, bạn thử lại sau nhé 🙂";

            return candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
        }
    }

    public class ChatRequest
    {
        public string message { get; set; } = "";
    }
}
