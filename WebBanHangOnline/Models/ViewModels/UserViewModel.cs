namespace WebBanHangOnline.Models.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }

        public bool IsAdmin { get; set; }
        public bool IsClient { get; set; }
        public bool IsLocked { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
    }
}
