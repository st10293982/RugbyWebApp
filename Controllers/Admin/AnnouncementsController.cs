// Controllers/Admin/AnnouncementsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PassItOnAcademy.Data;
using PassItOnAcademy.Models;

namespace PassItOnAcademy.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AnnouncementsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _email;

        public AnnouncementsController(ApplicationDbContext db, IEmailSender email)
        { _db = db; _email = email; }

        // GET: /Announcements
        public async Task<IActionResult> Index(int? sessionId)
        {
            var q = _db.Announcements
                .Include(a => a.Session)
                .OrderByDescending(a => a.CreatedUtc)
                .AsNoTracking();

            if (sessionId is not null) q = q.Where(a => a.SessionId == sessionId || a.IsGlobal);

            return View(await q.Take(500).ToListAsync());
        }

        // GET: /Announcements/Create
        public IActionResult Create(int? sessionId)
        {
            ViewBag.SessionId = sessionId;
            return View();
        }

        // POST: /Announcements/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Body,IsGlobal,SessionId")] Announcement a)
        {
            if (!a.IsGlobal && a.SessionId is null)
                ModelState.AddModelError("SessionId", "Select a session or mark as global.");

            if (!ModelState.IsValid) return View(a);

            a.CreatedByUserId = User.GetUserId()!; // helper below
            _db.Announcements.Add(a);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Announcement created.";
            return RedirectToAction(nameof(Details), new { id = a.Id });
        }

        // GET: /Announcements/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var a = await _db.Announcements.Include(x => x.Session).FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();
            return View(a);
        }

        // POST: /Announcements/Notify/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Notify(int id)
        {
            var a = await _db.Announcements.Include(x => x.Session).FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();

            var recipients = new List<ApplicationUser>();

            if (a.IsGlobal)
            {
                // Next 30 days sessions’ booked users
                var from = DateTime.UtcNow;
                var to = from.AddDays(30);
                recipients = await _db.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Session)
                    .Where(b => b.Status == BookingStatus.Booked
                             && b.Session!.StartUtc >= from && b.Session.StartUtc <= to)
                    .Select(b => b.User!)
                    .Distinct()
                    .Where(u => u.Email != null)
                    .ToListAsync();
            }
            else
            {
                if (a.SessionId is null) return BadRequest();
                recipients = await _db.Bookings
                    .Include(b => b.User)
                    .Where(b => b.SessionId == a.SessionId && b.Status == BookingStatus.Booked && b.User!.Email != null)
                    .Select(b => b.User!)
                    .Distinct()
                    .ToListAsync();
            }

            int sent = 0;
            foreach (var u in recipients)
            {
                try
                {
                    await _email.SendEmailAsync(u.Email!, $"[PassItOnAcademy] {a.Title}", a.Body);
                    sent++;
                }
                catch { /* swallow for MVP */ }
            }

            a.RecipientsNotified += sent;
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Notification sent to {sent} recipients.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Announcements/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var a = await _db.Announcements.FindAsync(id);
            if (a == null) return NotFound();
            _db.Announcements.Remove(a);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Announcement deleted.";
            return RedirectToAction(nameof(Index));
        }
    }

    internal static class ClaimsHelpers
    {
        public static string? GetUserId(this System.Security.Claims.ClaimsPrincipal user) =>
            user.Claims.FirstOrDefault(c => c.Type.EndsWith("/nameidentifier"))?.Value;
    }
}
