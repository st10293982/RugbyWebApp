using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;                     // <-- needed for Include/ThenInclude/SumAsync
using PassItOnAcademy.Data;
using PassItOnAcademy.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PassItOnAcademy.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PaymentsController(ApplicationDbContext db) => _db = db;

        // GET: /Payments
        public async Task<IActionResult> Index(int? sessionId, DateTime? from, DateTime? to, string? status)
        {
            var qry = _db.Payments
                .Include(p => p.Booking).ThenInclude(b => b.User)
                .Include(p => p.Booking).ThenInclude(b => b.Session)
                .AsNoTracking()
                .AsQueryable();

            if (sessionId is not null) qry = qry.Where(p => p.Booking.SessionId == sessionId);
            if (from is not null) qry = qry.Where(p => p.CreatedUtc >= from.Value.ToUniversalTime());
            if (to is not null) qry = qry.Where(p => p.CreatedUtc < to.Value.ToUniversalTime());
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var st))
                qry = qry.Where(p => p.Status == st);

            var items = await qry.OrderByDescending(p => p.CreatedUtc).Take(1000).ToListAsync();

            // ----- Totals (Expected = sum of Session.Price for matching bookings) -----
            var expected = await _db.Bookings
                .Where(b => sessionId == null || b.SessionId == sessionId)
                .Where(b => from == null || b.CreatedUtc >= DateTime.SpecifyKind(from.Value, DateTimeKind.Local).ToUniversalTime())
                .Where(b => to == null || b.CreatedUtc < DateTime.SpecifyKind(to.Value, DateTimeKind.Local).ToUniversalTime())
                 .Select(b => (decimal?)(b.Session != null ? b.Session.Price : 0m))
                .SumAsync() ?? 0m;

            var paid = items.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount);

            ViewBag.Expected = expected;
            ViewBag.Paid = paid;

            return View(items); // Views/Admin/Payments/Index.cshtml
        }

        // GET: /Payments/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var p = await _db.Payments
                .Include(x => x.Booking).ThenInclude(b => b.User)
                .Include(x => x.Booking).ThenInclude(b => b.Session)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null) return NotFound();
            return View(p);
        }
    }
}