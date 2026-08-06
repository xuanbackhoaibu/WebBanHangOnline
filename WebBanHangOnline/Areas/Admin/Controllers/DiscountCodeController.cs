using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DiscountCodeController : Controller
{
    private readonly ApplicationDbContext _context;

    public DiscountCodeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var codes = await _context.DiscountCodes
            .OrderByDescending(code => code.CreatedAt)
            .ToListAsync();

        return View(codes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DiscountCode model)
    {
        model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
        model.Description = model.Description?.Trim() ?? string.Empty;
        model.CreatedAt = DateTime.Now;

        if (!DiscountTypes.All.Contains(model.DiscountType))
        {
            ModelState.AddModelError(nameof(model.DiscountType), "Kiểu giảm giá không hợp lệ.");
        }

        if (model.DiscountValue <= 0)
        {
            ModelState.AddModelError(nameof(model.DiscountValue), "Giá trị giảm phải lớn hơn 0.");
        }

        if (string.IsNullOrWhiteSpace(model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "Vui lòng nhập mã giảm giá.");
        }

        if (model.DiscountType == DiscountTypes.Percent && model.DiscountValue > 100)
        {
            ModelState.AddModelError(nameof(model.DiscountValue), "Giảm theo phần trăm không được vượt quá 100%.");
        }

        if (model.StartsAt.HasValue && model.EndsAt.HasValue && model.EndsAt <= model.StartsAt)
        {
            ModelState.AddModelError(nameof(model.EndsAt), "Thời gian kết thúc phải sau thời gian bắt đầu.");
        }

        if (await _context.DiscountCodes.AnyAsync(code => code.Code == model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "Mã giảm giá đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = string.Join(" ", ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        _context.DiscountCodes.Add(model);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã tạo mã giảm giá {model.Code}.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var code = await _context.DiscountCodes.FindAsync(id);
        if (code == null)
        {
            return NotFound();
        }

        code.IsActive = !code.IsActive;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = code.IsActive ? "Đã bật mã giảm giá." : "Đã tắt mã giảm giá.";

        return RedirectToAction(nameof(Index));
    }
}
