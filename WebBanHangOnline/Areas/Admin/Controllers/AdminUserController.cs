using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebBanHangOnline.Models;

[Authorize(Roles = "Admin")]
public class AdminUserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminUserController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // 📋 Danh sách user
    public IActionResult Index()
    {
        var users = _userManager.Users.ToList();
        return View(users);
    }

    // 🔒 Khóa / mở khóa
    public async Task<IActionResult> ToggleLock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.LockoutEnd = user.LockoutEnd == null
            ? DateTimeOffset.MaxValue
            : null;

        await _userManager.UpdateAsync(user);
        return RedirectToAction(nameof(Index));
    }
}