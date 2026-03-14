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

            object reply;

            if (intent.Contains("tìm áo"))
                reply = QueryProduct("áo");

            else if (intent.Contains("tìm quần"))
                reply = QueryProduct("quần");

            else if (intent.Contains("giá rẻ"))
                reply = new
                {
                    text = "Shop có nhiều mẫu dưới 200k 🔥 Bạn muốn áo hay quần?"
                };

            else
                reply = new
                {
                    text = await AskAI(message)
                };

            await Clients.Caller.SendAsync("ReceiveMessage", user, reply);
        }

        // ===== AI hiểu ý định =====
        async Task<string> DetectIntent(string text)
        {
            var prompt = $"Phân loại ý định người dùng ngắn gọn: {text}. " +
                         $"Chỉ trả về: tìm áo, tìm quần, giá rẻ, trò chuyện";

            return await AskAI(prompt);
        }

        // ===== QUERY DATABASE =====
        object QueryProduct(string keyword)
        {
            var products = _db.Products
                              .Where(p => p.Name.ToLower().Contains(keyword))
                              .Take(3)
                              .Select(p => new
                              {
                                  name = p.Name,
                                  price = p.Price,
                                  image = p.Images.FirstOrDefault().ImageUrl,
                                  link = "/Product/Detail/" + p.ProductId
                              })
                              .ToList();

            if (!products.Any())
            {
                return new
                {
                    text = $"Hiện shop chưa có {keyword} phù hợp 😢"
                };
            }

            return new
            {
                text = $"Shop gợi ý {keyword} cho bạn:",
                products = products
            };
        }

        // ===== GỌI GEMINI AI =====
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
                .GetString() ?? "Shop chưa hiểu câu hỏi 🤔";
        }
    }
}