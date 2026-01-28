using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Controllers
{
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Support
        public async Task<IActionResult> Index()
        {
            var faqs = await _context.SupportFaqs
                .Where(x => x.IsActive)
                .ToListAsync();

            return View(faqs);
        }

        // GET: /Support/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Support/Create
        [HttpPost]
        public async Task<IActionResult> Create(SupportRequest model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.SupportRequests.Add(model);
            await _context.SaveChangesAsync();

            TempData["success"] = "Yêu cầu của bạn đã được gửi. Chúng tôi sẽ liên hệ sớm!";
            return RedirectToAction(nameof(Create));
        }

        // GET: /Support/Track
        public IActionResult Track()
        {
            return View();
        }

        // POST: /Support/Track
        [HttpPost]
        public async Task<IActionResult> Track(string email)
        {
            var requests = await _context.SupportRequests
                .Where(x => x.Email == email)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View("TrackResult", requests);
        }
    }
}