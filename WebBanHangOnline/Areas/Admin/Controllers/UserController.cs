using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;
using WebBanHangOnline.Models.ViewModels;

namespace WebBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // ===============================
        // 📄 DANH SÁCH USER
        // ===============================
        public async Task<IActionResult> Index(string? search, string? role, string? status)
        {
            var users = await _userManager.Users
                .OrderByDescending(user => user.CreatedDate)
                .ToListAsync();

            var orderStats = await _context.Orders
                .Where(order =>
                    OrderStatuses.RevenueStatuses.Contains(order.Status) ||
                    order.PaymentStatus == PaymentStatuses.Paid)
                .GroupBy(order => order.UserId)
                .Select(group => new
                {
                    UserId = group.Key,
                    OrderCount = group.Count(),
                    TotalSpent = group.Sum(order => order.TotalAmount),
                    LastOrderDate = group.Max(order => order.OrderDate)
                })
                .ToDictionaryAsync(item => item.UserId);

            var result = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                orderStats.TryGetValue(user.Id, out var stats);

                result.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    CreatedDate = user.CreatedDate,
                    LastOrderDate = stats?.LastOrderDate,
                    OrderCount = stats?.OrderCount ?? 0,
                    TotalSpent = stats?.TotalSpent ?? 0,
                    IsAdmin = roles.Contains("Admin"),
                    IsClient = roles.Contains("Client"),
                    IsLocked = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.Now,
                    LockoutEnd = user.LockoutEnd
                });
            }

            var allUsers = result;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                result = result.Where(user =>
                    user.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    user.UserName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    user.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (user.PhoneNumber ?? string.Empty).Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                result = role switch
                {
                    "Admin" => result.Where(user => user.IsAdmin).ToList(),
                    "Client" => result.Where(user => user.IsClient && !user.IsAdmin).ToList(),
                    "User" => result.Where(user => !user.IsAdmin && !user.IsClient).ToList(),
                    _ => result
                };
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                result = status switch
                {
                    "Locked" => result.Where(user => user.IsLocked).ToList(),
                    "Active" => result.Where(user => !user.IsLocked).ToList(),
                    _ => result
                };
            }

            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.Status = status;
            ViewBag.TotalUsers = allUsers.Count;
            ViewBag.ActiveUsers = allUsers.Count(user => !user.IsLocked);
            ViewBag.LockedUsers = allUsers.Count(user => user.IsLocked);
            ViewBag.ClientUsers = allUsers.Count(user => user.IsClient && !user.IsAdmin);
            ViewBag.TotalFilteredUsers = result.Count;
            ViewBag.TotalCustomerValue = result.Sum(user => user.TotalSpent);

            return View(result);
        }

        // ===============================
        // 🔒 / 🔓 KHÓA USER
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Không thể khóa Admin";
                return RedirectToAction(nameof(Index));
            }

            user.LockoutEnd = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.Now
                ? null
                : DateTimeOffset.Now.AddYears(100);

            await _userManager.UpdateAsync(user);
            TempData["Success"] = user.LockoutEnd == null ? "Đã mở khóa người dùng." : "Đã khóa người dùng.";
            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // ⭐ CẤP CLIENT
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantClient(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToAction(nameof(Index));

            if (!await _userManager.IsInRoleAsync(user, "Client"))
                await _userManager.AddToRoleAsync(user, "Client");

            TempData["Success"] = "Đã cấp vai trò Client.";
            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // 🔁 THU HỒI CLIENT → USER
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeClient(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Client"))
                await _userManager.RemoveFromRoleAsync(user, "Client");

            TempData["Success"] = "Đã thu hồi vai trò Client.";
            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // ❌ XÓA USER
        // ===============================
        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(string id)
{
    var user = await _userManager.FindByIdAsync(id);
    if (user == null) return NotFound();

    if (await _userManager.IsInRoleAsync(user, "Admin"))
    {
        TempData["Error"] = "Không thể xóa Admin";
        return RedirectToAction(nameof(Index));
    }

    var result = await _userManager.DeleteAsync(user);

    if (!result.Succeeded)
    {
        TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
    }
    else
    {
        TempData["Success"] = "Xóa user thành công";
    }

    return RedirectToAction(nameof(Index));
}
    }
}
