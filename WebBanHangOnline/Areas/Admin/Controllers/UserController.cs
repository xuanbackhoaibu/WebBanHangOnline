using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebBanHangOnline.Models;
using WebBanHangOnline.Models.ViewModels;

namespace WebBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // ===============================
        // DANH SÁCH USER
        // ===============================
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var result = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                result.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    IsAdmin = roles.Contains("Admin"),
                    IsClient = roles.Contains("Client"),
                    IsLocked = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.Now
                });
            }

            return View(result);
        }

        // ===============================
        // KHÓA / MỞ USER
        // ===============================
        [HttpPost]
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
            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // CẤP CLIENT
        // ===============================
        [HttpPost]
        public async Task<IActionResult> GrantClient(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Không thể cấp Client cho Admin";
                return RedirectToAction(nameof(Index));
            }

            if (!await _userManager.IsInRoleAsync(user, "Client"))
            {
                await _userManager.AddToRoleAsync(user, "Client");
            }

            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // XOÁ USER
        // ===============================
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Không thể xoá Admin";
                return RedirectToAction(nameof(Index));
            }

            await _userManager.DeleteAsync(user);
            return RedirectToAction(nameof(Index));
        }
    }
}
