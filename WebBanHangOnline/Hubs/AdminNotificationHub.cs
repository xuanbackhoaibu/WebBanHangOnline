using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebBanHangOnline.Hubs;

[Authorize(Roles = "Admin")]
public class AdminNotificationHub : Hub
{
}
