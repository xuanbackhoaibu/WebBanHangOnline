using Microsoft.AspNetCore.Mvc;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace WebBanHangOnline.Controllers
{
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Xem danh sách
        public async Task<IActionResult> Index(string type)
        {
            var query = _context.Notifications.AsQueryable();

            if (!string.IsNullOrEmpty(type))
                query = query.Where(n => n.Type == type);

            var notifications = await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
            return View(notifications);
        }

        // Chi tiết
        public async Task<IActionResult> Details(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();
            return View(notification);
        }
    }
}