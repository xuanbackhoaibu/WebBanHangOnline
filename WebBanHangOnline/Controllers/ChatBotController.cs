using Microsoft.AspNetCore.Mvc;

namespace WebBanHangOnline.Controllers
{
    public class ChatBotController : Controller
    {
        private readonly HttpClient _http = new();

        [HttpPost]
        public async Task<IActionResult> Reply([FromBody] string msg)
        {
            msg = msg.ToLower();

            if (msg.Contains("ship"))
                return Content("Shop miễn phí ship cho đơn trên 300k");

            if (msg.Contains("giờ"))
                return Content("Shop mở cửa từ 8h đến 22h");

            return Content(await AskAI(msg));
        }

        async Task<string> AskAI(string question)
        {
            _http.DefaultRequestHeaders.Add("Authorization",
                "Bearer AIzaSyAirWq66QE7azTiRGUP6RBxtWt5jmlZm84");

            var body = new
            {
                model = "gpt-4.1-mini",
                messages = new[] {
                new { role="user", content = question }
            }
            };

            var res = await _http.PostAsJsonAsync(
                "https://api.openai.com/v1/chat/completions", body);

            dynamic data = await res.Content.ReadFromJsonAsync<dynamic>();
            return data.choices[0].message.content;
        }
    }
}
