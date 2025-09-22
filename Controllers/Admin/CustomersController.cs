// Controllers/Admin/CustomersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PassItOnAcademy.Data;

namespace PassItOnAcademy.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CustomersController(ApplicationDbContext db) => _db = db;

        // GET: /Customers
        public async Task<IActionResult> Index(string? q)
        {
            var users = _db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                users = users.Where(u =>
                    (u.FullName != null && u.FullName.Contains(q)) ||
                    (u.Email != null && u.Email.Contains(q)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(q)));
            }

            var result = await users
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    TotalBookings = _db.Bookings.Count(b => b.UserId == u.Id)
                })
                .OrderByDescending(x => x.TotalBookings)
                .Take(500)
                .ToListAsync();

            return View(result);
        }

        // GET: /Customers/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var bookings = await _db.Bookings
                .Include(b => b.Session)
                .Where(b => b.UserId == id)
                .OrderByDescending(b => b.CreatedUtc)
                .ToListAsync();

            ViewBag.Bookings = bookings;
            return View(user);
        }
    }
}
