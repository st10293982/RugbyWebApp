using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PassItOnAcademy.Data;
using PassItOnAcademy.Models;
using PassItOnAcademy.ViewModels;

namespace PassItOnAcademy.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin/messages")]
    public class ContactMessagesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ContactMessagesController(ApplicationDbContext db) => _db = db;

        // GET: /admin/messages
        [HttpGet("")]
        public async Task<IActionResult> Index(string? q, ContactStatus? status, int page = 1, int pageSize = 20)
        {
            var query = _db.ContactMessages.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(m =>
                    m.Name.ToLower().Contains(term) ||
                    m.Email.ToLower().Contains(term) ||
                    (m.Phone ?? "").ToLower().Contains(term) ||
                    m.Subject.ToLower().Contains(term) ||
                    m.Message.ToLower().Contains(term));
            }

            if (status.HasValue)
                query = query.Where(m => m.Status == status.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(m => m.CreatedUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new AdminMessageListItemVM
                {
                    Id = m.Id,
                    Name = m.Name,
                    Email = m.Email,
                    Phone = m.Phone,
                    Subject = m.Subject,
                    Snippet = m.Message.Length > 120 ? m.Message.Substring(0, 120) + "…" : m.Message,
                    CreatedLocal = DateTime.SpecifyKind(m.CreatedUtc, DateTimeKind.Utc).ToLocalTime(),
                    Status = m.Status
                })
                .ToListAsync();

            var vm = new AdminMessageIndexVM
            {
                Q = q,
                Status = status,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };

            return View(vm);
        }

        // GET: /admin/messages/123
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var m = await _db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id);
            if (m == null) return NotFound();

            // auto-mark as Read when opened (optional)
            if (m.Status == ContactStatus.New)
            {
                m.Status = ContactStatus.Read;
                await _db.SaveChangesAsync();
            }

            var vm = new AdminMessageDetailsVM
            {
                Id = m.Id,
                Name = m.Name,
                Email = m.Email,
                Phone = m.Phone,
                Subject = m.Subject,
                Message = m.Message,
                CreatedLocal = DateTime.SpecifyKind(m.CreatedUtc, DateTimeKind.Utc).ToLocalTime(),
                Status = m.Status
            };

            return View(vm);
        }

        // POST: /admin/messages/123/read
        [HttpPost("{id:int}/read")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var m = await _db.ContactMessages.FindAsync(id);
            if (m == null) return NotFound();
            m.Status = ContactStatus.Read;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /admin/messages/123/archive
        [HttpPost("{id:int}/archive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var m = await _db.ContactMessages.FindAsync(id);
            if (m == null) return NotFound();
            m.Status = ContactStatus.Archived;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: /admin/messages/123/delete
        [HttpPost("{id:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.ContactMessages.FindAsync(id);
            if (m == null) return NotFound();
            _db.ContactMessages.Remove(m);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
