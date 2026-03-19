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
        // 📄 DANH SÁCH USER
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
        // 🔒 / 🔓 KHÓA USER
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
        // ⭐ CẤP CLIENT
        // ===============================
        [HttpPost]
        public async Task<IActionResult> GrantClient(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToAction(nameof(Index));

            if (!await _userManager.IsInRoleAsync(user, "Client"))
                await _userManager.AddToRoleAsync(user, "Client");

            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // 🔁 THU HỒI CLIENT → USER
        // ===============================
        [HttpPost]
        public async Task<IActionResult> RevokeClient(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Client"))
                await _userManager.RemoveFromRoleAsync(user, "Client");

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
