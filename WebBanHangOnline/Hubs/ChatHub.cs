using Microsoft.AspNetCore.SignalR;
using System.Text;
using System.Text.Json;
using WebBanHangOnline.Data;

namespace WebBanHangOnline.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly HttpClient _http = new HttpClient();

        public ChatHub(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task SendMessage(string user, string message)
        {
            var intent = await DetectIntent(message);

            string reply;

            if (intent.Contains("tìm áo"))
                reply = QueryProduct("áo");

            else if (intent.Contains("tìm quần"))
                reply = QueryProduct("quần");

            else if (intent.Contains("giá rẻ"))
                reply = "Shop có nhiều mẫu dưới 200k rất đẹp 🔥 Bạn muốn áo hay quần ạ?";

            else
                reply = await AskAI(message);

            await Clients.Caller.SendAsync("ReceiveMessage", user, reply);
        }

        // ===== AI hiểu ý định =====
        async Task<string> DetectIntent(string text)
        {
            var prompt = $"Phân loại ý định người dùng ngắn gọn: {text}. " +
                         $"Chỉ trả về: tìm áo, tìm quần, giá rẻ, trò chuyện";

            return await AskAI(prompt);
        }

        // ===== QUERY DB =====
        string QueryProduct(string keyword)
        {
            var products = _db.Products
                              .Where(p => p.Name.Contains(keyword))
                              .Take(3)
                              .ToList();

            if (!products.Any())
                return $"Hiện shop chưa có {keyword} phù hợp 😢";

            var sb = new StringBuilder($"Shop gợi ý {keyword} cho bạn:\n");

            foreach (var p in products)
                sb.AppendLine($"• {p.Name} – {p.Price:N0}đ");

            sb.Append("\nBạn thích phong cách nào ạ?");

            return sb.ToString();
        }

        // ===== GỌI AI =====
        async Task<string> AskAI(string question)
        {
            var apiKey = _config["Gemini:ApiKey"];

            var body = new
            {
                contents = new[]
                {
                    new {
                        parts = new[] {
                            new { text = question }
                        }
                    }
                }
            };

            var res = await _http.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}",
                new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            );

            var json = await res.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
        }
    }
}
