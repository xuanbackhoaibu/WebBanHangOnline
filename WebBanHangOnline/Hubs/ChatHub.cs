using Microsoft.AspNetCore.SignalR;
using WebBanHangOnline.Data;

namespace WebBanHangOnline.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _db;

        public ChatHub(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task SendMessage(string user, string message)
        {
            string reply;

            var msg = message.ToLower();

            if (msg.Contains("áo"))
            {
                var products = _db.Products
                                  .Where(p => p.Name.Contains("áo"))
                                  .Take(3)
                                  .ToList();

                if (!products.Any())
                    reply = "Hiện shop chưa có áo phù hợp 😢";
                else
                {
                    reply = "Shop gợi ý cho bạn:\n";
                    foreach (var p in products)
                        reply += $"- {p.Name} ({p.Price:N0}đ)\n";
                }
            }
            else
            {
                reply = "Bạn muốn tìm sản phẩm gì nữa không ạ?";
            }

            await Clients.Caller.SendAsync("ReceiveMessage", user, reply);
        }
    }
}
