namespace WebBanHangOnline.Models
{
    public class ChatHistory
    {
        public int ProductId { get; set; }
        public string Message { get; set; }
        public bool IsUser { get; set; }
        public DateTime Time { get; set; }
    }
}
