using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PassItOnAcademy.Data;
using PassItOnAcademy.Models;

namespace PassItOnAcademy.Controllers.Admin
{
    [Authorize(Roles = "Admin")] // only admins manage programs
    public class ProgramsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ProgramsController(ApplicationDbContext db) => _db = db;

        // GET: /Programs
        public async Task<IActionResult> Index(string? q, string show = "active")
        {
            var query = _db.TrainingPrograms.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p => p.Name.Contains(q) || (p.Description != null && p.Description.Contains(q)));
            }

            show = (show ?? "active").ToLowerInvariant();
            if (show == "inactive") query = query.Where(p => !p.IsActive);
            else if (show == "all") { /* no filter */ }
            else query = query.Where(p => p.IsActive);

            var items = await query.OrderBy(p => p.Name).ToListAsync();
            ViewBag.Show = show;
            ViewBag.Query = q;
            return View(items); // Views/Admin/Programs/Index.cshtml
        }

        // GET: /Programs/Create
        public IActionResult Create() => View();

        // POST: /Programs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Level,DurationMinutes,Price,IsActive")] TrainingProgram model)
        {
            if (!ModelState.IsValid) return View(model);

            model.Name = model.Name.Trim();
            if (!string.IsNullOrWhiteSpace(model.Level)) model.Level = model.Level!.Trim();

            _db.TrainingPrograms.Add(model);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Program created.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Programs/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var p = await _db.TrainingPrograms.FindAsync(id);
            if (p == null) return NotFound();
            return View(p);
        }

        // POST: /Programs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Level,DurationMinutes,Price,IsActive")] TrainingProgram model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var p = await _db.TrainingPrograms.FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();

            p.Name = model.Name.Trim();
            p.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            p.Level = string.IsNullOrWhiteSpace(model.Level) ? null : model.Level.Trim();
            p.DurationMinutes = model.DurationMinutes;
            p.Price = model.Price;
            p.IsActive = model.IsActive;

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Program updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Programs/Toggle/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var p = await _db.TrainingPrograms.FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();

            p.IsActive = !p.IsActive;
            await _db.SaveChangesAsync();
            TempData["Msg"] = p.IsActive ? "Program activated." : "Program deactivated.";
            return RedirectToAction(nameof(Index), new { show = "all" });
        }

        // POST: /Programs/Delete/5
        // (Soft delete via IsActive is safer. If you truly want hard delete, keep this.)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _db.TrainingPrograms
                .Include(x => x.Sessions)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null) return NotFound();

            if (p.Sessions.Any())
            {
                TempData["Msg"] = "Cannot delete: program has sessions. Deactivate instead.";
                return RedirectToAction(nameof(Index));
            }

            _db.TrainingPrograms.Remove(p);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Program deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
