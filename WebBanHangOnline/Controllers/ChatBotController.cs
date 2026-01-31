using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using WebBanHangOnline.Data;

namespace WebBanHangOnline.Controllers
{
    public class ChatBotController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly HttpClient _http = new();

        public ChatBotController(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] ChatRequest req)
        {
            var msg = req.message.ToLower();

            // ===== QUICK RULES =====
            if (msg.Contains("ship"))
                return Json(new { reply = "Shop miễn phí ship cho đơn trên 300k nhé!" });

            if (msg.Contains("giờ"))
                return Json(new { reply = "Shop mở cửa từ 8h đến 22h mỗi ngày!" });

            // ===== DATABASE SEARCH =====
            if (msg.Contains("áo"))
            {
                var products = _db.Products
                                  .Where(p => p.Name.Contains("áo"))
                                  .Take(3)
                                  .ToList();

                if (!products.Any())
                    return Json(new { reply = "Hiện shop chưa có áo phù hợp 😢" });

                string text = "Shop gợi ý cho bạn:\n";

                foreach (var p in products)
                    text += $"- {p.Name} ({p.Price:N0}đ)\n";

                return Json(new { reply = text });
            }

            // ===== AI FALLBACK =====
            string aiReply = await AskGemini(req.message);
            return Json(new { reply = aiReply });
        }

        // ================= AI =================
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
                                text = $@"Bạn là trợ lý tư vấn shop thời trang XuanBac.
Tư vấn thân thiện, gợi ý mua hàng.
Nếu hỏi ngoài mua sắm thì kéo về sản phẩm.

Khách hỏi: {question}"
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(body);

            var response = await _http.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}",
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
                .GetString() ?? "Shop chưa hiểu câu hỏi của bạn 🤔";
        }
    }

    public class ChatRequest
    {
        public string message { get; set; } = "";
    }
}
