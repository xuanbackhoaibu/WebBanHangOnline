using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================
        // FAQ
        // ======================

        public async Task<IActionResult> Faq()
        {
            var faqs = await _context.SupportFaqs.ToListAsync();
            return View(faqs);
        }

        public IActionResult CreateFaq()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateFaq(SupportFaq model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.SupportFaqs.Add(model);
            await _context.SaveChangesAsync();

            TempData["success"] = "Đã thêm FAQ";
            return RedirectToAction(nameof(Faq));
        }

        public async Task<IActionResult> EditFaq(int id)
        {
            var faq = await _context.SupportFaqs.FindAsync(id);
            if (faq == null) return NotFound();

            return View(faq);
        }

        [HttpPost]
        public async Task<IActionResult> EditFaq(SupportFaq model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.SupportFaqs.Update(model);
            await _context.SaveChangesAsync();

            TempData["success"] = "Đã cập nhật FAQ";
            return RedirectToAction(nameof(Faq));
        }

        public async Task<IActionResult> ToggleFaq(int id)
        {
            var faq = await _context.SupportFaqs.FindAsync(id);
            if (faq == null) return NotFound();

            faq.IsActive = !faq.IsActive;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Faq));
        }

        public async Task<IActionResult> DeleteFaq(int id)
        {
            var faq = await _context.SupportFaqs.FindAsync(id);
            if (faq == null) return NotFound();

            _context.SupportFaqs.Remove(faq);
            await _context.SaveChangesAsync();

            TempData["success"] = "Đã xoá FAQ";
            return RedirectToAction(nameof(Faq));
        }

        // ======================
        // SUPPORT REQUEST
        // ======================

        public async Task<IActionResult> Requests()
        {
            var requests = await _context.SupportRequests
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var req = await _context.SupportRequests.FindAsync(id);
            if (req == null) return NotFound();

            req.Status = status;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Requests));
        }

        public async Task<IActionResult> DeleteRequest(int id)
        {
            var req = await _context.SupportRequests.FindAsync(id);
            if (req == null) return NotFound();

            _context.SupportRequests.Remove(req);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Requests));
        }
    }
}
