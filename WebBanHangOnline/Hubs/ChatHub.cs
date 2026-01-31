using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WebBanHangOnline.Controllers;

namespace WebBanHangOnline.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatBotController _bot;

        public ChatHub(ChatBotController bot)
        {
            _bot = bot;
        }

        public async Task SendMessage(string message)
        {
            var result = await _bot.Send(new ChatRequest { message = message });

            if (result is JsonResult json)
            {
                var reply = json.Value?.ToString() ?? "Shop chưa hiểu câu hỏi 😊";

                await Clients.Caller.SendAsync("ReceiveMessage", reply);
            }
        }
    }
}
