using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;

namespace WebBanHangOnline.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AuditController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? entity, string? action, int page = 1)
    {
        const int pageSize = 30;
        page = Math.Max(1, page);

        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entity))
        {
            query = query.Where(log => log.EntityName == entity);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(log => log.Action == action);
        }

        var totalItems = await query.CountAsync();
        var logs = await query
            .OrderByDescending(log => log.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Entities = await _context.AuditLogs
            .AsNoTracking()
            .Select(log => log.EntityName)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync();
        ViewBag.Actions = await _context.AuditLogs
            .AsNoTracking()
            .Select(log => log.Action)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync();
        ViewBag.Entity = entity;
        ViewBag.Action = action;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return View(logs);
    }
}
