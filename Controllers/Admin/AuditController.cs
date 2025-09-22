using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PassItOnAcademy.Data;

namespace PassItOnAcademy.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AuditController : Controller
    {
        private readonly ApplicationDbContext _db;
        public AuditController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var items = await _db.AuditEntries
                .Include(a => a.PerformedByUser)
                .OrderByDescending(a => a.CreatedUtc)
                .Take(500)
                .ToListAsync();
            return View(items);
        }
    }
}
