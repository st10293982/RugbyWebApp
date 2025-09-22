// Controllers/Admin/SettingsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PassItOnAcademy.Data;
using PassItOnAcademy.Models;

namespace PassItOnAcademy.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public SettingsController(ApplicationDbContext db) => _db = db;

        private async Task<AcademySetting> LoadAsync()
        {
            var s = await _db.AcademySettings.FirstOrDefaultAsync();
            if (s == null) { s = new AcademySetting(); _db.AcademySettings.Add(s); await _db.SaveChangesAsync(); }
            return s;
        }

        public async Task<IActionResult> Index() => View(await LoadAsync());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AcademySetting model)
        {
            if (!ModelState.IsValid) return View(model);
            var s = await LoadAsync();
            _db.Entry(s).CurrentValues.SetValues(model);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Settings saved.";
            return RedirectToAction(nameof(Index));
        }
    }
}
