using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PassItOnAcademy.Data;
using PassItOnAcademy.Models;

namespace PassItOnAcademy.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BookingsController(ApplicationDbContext db) => _db = db;

        // GET: /Bookings
        public async Task<IActionResult> Index(int? sessionId, DateTime? from, DateTime? to, string? status, string? q)
        {
            var qry = _db.Bookings
                .Include(b => b.User)
                .Include(b => b.Session)
                .Include(b => b.Payments)
                .AsNoTracking()
                .AsQueryable();

            if (sessionId is not null) qry = qry.Where(b => b.SessionId == sessionId);
            if (from is not null) qry = qry.Where(b => b.CreatedUtc >= from.Value.ToUniversalTime());
            if (to is not null) qry = qry.Where(b => b.CreatedUtc < to.Value.ToUniversalTime());
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<BookingStatus>(status, true, out var st))
                    qry = qry.Where(b => b.Status == st);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                qry = qry.Where(b =>
                    (b.User != null && (
                        (b.User.FullName != null && b.User.FullName.Contains(q)) ||
                        (b.User.PhoneNumber != null && b.User.PhoneNumber.Contains(q)) ||
                        (b.User.Email != null && b.User.Email.Contains(q))
                    )));
            }

            var items = await qry
                .OrderByDescending(b => b.CreatedUtc)
                .Take(500) // guardrails
                .ToListAsync();

            return View(items); // Views/Admin/Bookings/Index.cshtml
        }

        // GET: /Bookings/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var b = await _db.Bookings
                .Include(x => x.User)
                .Include(x => x.Session)
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (b == null) return NotFound();
            return View(b);
        }

        // POST: /Bookings/Cancel/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var b = await _db.Bookings.Include(x => x.Session).FirstOrDefaultAsync(x => x.Id == id);
            if (b == null) return NotFound();
            if (b.Status != BookingStatus.Booked) return BadRequest("Only booked can be cancelled.");

            b.Status = BookingStatus.Cancelled;
            // TODO: add Audit row (who, when, reason)
            await _db.SaveChangesAsync();

            // TODO: notify customer (email)
            TempData["Msg"] = "Booking cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Bookings/Complete/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var b = await _db.Bookings.FindAsync(id);
            if (b == null) return NotFound();
            if (b.Status != BookingStatus.Booked) return BadRequest("Only booked can be completed.");

            b.Status = BookingStatus.Completed;
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Marked completed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Bookings/Move/5
        public async Task<IActionResult> Move(int id)
        {
            var b = await _db.Bookings.Include(x => x.Session).FirstOrDefaultAsync(x => x.Id == id);
            if (b == null) return NotFound();

            // candidate sessions (future, same level if you want)
            var candidates = await _db.TrainingSessions
                .Where(s => s.Status == SessionStatus.Scheduled && s.StartUtc > DateTime.UtcNow)
                .OrderBy(s => s.StartUtc)
                .Select(s => new {
                    s.Id,
                    s.Title,
                    s.Level,
                    s.StartUtc,
                    s.Capacity,
                    Booked = s.Bookings.Count(x => x.Status == BookingStatus.Booked)
                })
                .ToListAsync();

            ViewBag.Candidates = candidates
                .Where(c => c.Booked < c.Capacity)
                .ToList();

            return View(b); // show a dropdown/radio of target sessions
        }

        // POST: /Bookings/Move/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Move(int id, int targetSessionId)
        {
            var b = await _db.Bookings.Include(x => x.Session).FirstOrDefaultAsync(x => x.Id == id);
            if (b == null) return NotFound();
            if (b.Status != BookingStatus.Booked) return BadRequest("Only booked can be moved.");

            var target = await _db.TrainingSessions
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(s => s.Id == targetSessionId && s.Status == SessionStatus.Scheduled);
            if (target == null) return BadRequest("Target session not available.");

            var bookedCount = target.Bookings.Count(x => x.Status == BookingStatus.Booked);
            if (bookedCount >= target.Capacity) return BadRequest("Target session is full.");

            b.SessionId = target.Id;
            await _db.SaveChangesAsync();

            // TODO: notify customer of new time
            TempData["Msg"] = "Booking moved.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
