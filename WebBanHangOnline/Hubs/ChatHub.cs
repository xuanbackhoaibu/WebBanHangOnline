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

        public async Task SendMessage(string user, string message)
        {
            var result = await _bot.Send(new ChatRequest { message = message });

            if (result is OkObjectResult ok)
            {
                var reply = ok.Value.ToString();
                await Clients.Caller.SendAsync("ReceiveMessage", user, reply);
            }
        }
    }
}
