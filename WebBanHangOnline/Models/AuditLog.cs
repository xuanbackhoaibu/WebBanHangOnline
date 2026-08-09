namespace WebBanHangOnline.Models;

public class AuditLog
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = "System";
    public string Roles { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string OldValues { get; set; } = "{}";
    public string NewValues { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
