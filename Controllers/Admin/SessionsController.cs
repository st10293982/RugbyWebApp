using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using PassItOnAcademy.Data;
using PassItOnAcademy.Models;
using PassItOnAcademy.ViewModels;

namespace PassItOnAcademy.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class SessionsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public SessionsController(ApplicationDbContext db, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _env = env;
            _userManager = userManager;
        }

        // GET: /Admin/Sessions
        public async Task<IActionResult> Index(DateTime? from, DateTime? to, string? level)
        {
            // Query sessions with booking counts
            var q = _db.TrainingSessions
                .AsNoTracking()
                .Include(s => s.TrainingProgram)
                .Include(s => s.Bookings)
                .OrderBy(s => s.StartUtc)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(level))
                q = q.Where(s => s.Level == level);

            if (from.HasValue)
                q = q.Where(s => s.StartUtc >= from.Value.ToUniversalTime());

            if (to.HasValue)
                q = q.Where(s => s.StartUtc <= to.Value.ToUniversalTime());

            var items = await q.Select(s => new SessionListItemVM
            {
                Id = s.Id,
                Title = s.Title,
                Level = s.Level,
                StartUtc = s.StartUtc,
                EndUtc = s.EndUtc,
                Location = s.Location,
                Capacity = s.Capacity,
                BookedCount = s.Bookings.Count(b => b.Status == BookingStatus.Booked),
                Price = s.Price,
                Status = s.Status.ToString(),
                ImageUrl = s.ImageUrl
            }).ToListAsync();

            var levels = await _db.TrainingSessions
                .Where(s => s.Level != null)
                .Select(s => s.Level!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var vm = new SessionIndexVM
            {
                Filters = new SessionFiltersVM
                {
                    FromLocal = from,
                    ToLocal = to,
                    Level = level,
                    Levels = new SelectList(levels)
                },
                Items = items
            };

            return View(vm); // Views/Admin/Sessions/Index.cshtml
        }

        // GET: /Admin/Sessions/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var s = await _db.TrainingSessions
                .Include(x => x.TrainingProgram)
                .Include(x => x.Coach)
                .Include(x => x.Bookings).ThenInclude(b => b.User)
                .Include(x => x.Bookings).ThenInclude(b => b.Payments)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (s == null) return NotFound();

            return View(s); // Views/Admin/Sessions/Details.cshtml
        }

        // GET: /Admin/Sessions/Create
        public async Task<IActionResult> Create()
        {
            await PopulateProgramsAsync();
            var vm = new TrainingSessionFormVM
            {
                StartLocal = DateTime.Now.AddDays(1).Date.AddHours(16), // sensible default
                EndLocal = DateTime.Now.AddDays(1).Date.AddHours(17)
            };
            return View(vm); // Views/Admin/Sessions/Create.cshtml
        }

        // POST: /Admin/Sessions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TrainingSessionFormVM vm)
        {
            await PopulateProgramsAsync();
            if (!ModelState.IsValid) return View(vm);

            if (vm.EndLocal <= vm.StartLocal)
            {
                ModelState.AddModelError(nameof(vm.EndLocal), "End must be after start.");
                return View(vm);
            }

            // Capacity sanity
            if (vm.Capacity < 1)
            {
                ModelState.AddModelError(nameof(vm.Capacity), "Capacity must be at least 1.");
                return View(vm);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var entity = new TrainingSession
            {
                Title = vm.Title.Trim(),
                Level = string.IsNullOrWhiteSpace(vm.Level) ? null : vm.Level.Trim(),
                StartUtc = ToUtc(vm.StartLocal),
                EndUtc = ToUtc(vm.EndLocal),
                Location = string.IsNullOrWhiteSpace(vm.Location) ? null : vm.Location.Trim(),
                Capacity = vm.Capacity,
                Price = vm.Price,
                TrainingProgramId = vm.TrainingProgramId,
                CoachId = user.Id,
                ImageAlt = string.IsNullOrWhiteSpace(vm.ImageAlt) ? null : vm.ImageAlt.Trim()
            };

            // Handle image upload if provided
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                var uploadResult = await SaveImageAsync(vm.ImageFile);
                if (!uploadResult.ok)
                {
                    ModelState.AddModelError(nameof(vm.ImageFile), uploadResult.error!);
                    return View(vm);
                }
                entity.ImageUrl = uploadResult.url;
            }

            _db.TrainingSessions.Add(entity);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Session created.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Sessions/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            await PopulateProgramsAsync();

            var s = await _db.TrainingSessions.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();

            var vm = new TrainingSessionFormVM
            {
                Id = s.Id,
                Title = s.Title,
                Level = s.Level,
                StartLocal = FromUtcToLocal(s.StartUtc),
                EndLocal = FromUtcToLocal(s.EndUtc),
                Location = s.Location,
                Capacity = s.Capacity,
                Price = s.Price,
                TrainingProgramId = s.TrainingProgramId,
                ImageAlt = s.ImageAlt,
                ExistingImageUrl = s.ImageUrl
            };
            return View(vm); // Views/Admin/Sessions/Edit.cshtml
        }

        // POST: /Admin/Sessions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TrainingSessionFormVM vm)
        {
            await PopulateProgramsAsync();
            if (!ModelState.IsValid) return View(vm);

            var s = await _db.TrainingSessions.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();

            if (vm.EndLocal <= vm.StartLocal)
            {
                ModelState.AddModelError(nameof(vm.EndLocal), "End must be after start.");
                return View(vm);
            }

            if (vm.Capacity < 1)
            {
                ModelState.AddModelError(nameof(vm.Capacity), "Capacity must be at least 1.");
                return View(vm);
            }

            s.Title = vm.Title.Trim();
            s.Level = string.IsNullOrWhiteSpace(vm.Level) ? null : vm.Level.Trim();
            s.StartUtc = ToUtc(vm.StartLocal);
            s.EndUtc = ToUtc(vm.EndLocal);
            s.Location = string.IsNullOrWhiteSpace(vm.Location) ? null : vm.Location.Trim();
            s.Capacity = vm.Capacity;
            s.Price = vm.Price;
            s.TrainingProgramId = vm.TrainingProgramId;
            s.ImageAlt = string.IsNullOrWhiteSpace(vm.ImageAlt) ? null : vm.ImageAlt.Trim();

            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                var uploadResult = await SaveImageAsync(vm.ImageFile);
                if (!uploadResult.ok)
                {
                    ModelState.AddModelError(nameof(vm.ImageFile), uploadResult.error!);
                    return View(vm);
                }

                // Optionally delete old file
                if (!string.IsNullOrEmpty(s.ImageUrl))
                    DeleteFileIfExists(s.ImageUrl);

                s.ImageUrl = uploadResult.url;
            }

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Session updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Sessions/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var s = await _db.TrainingSessions
                .Include(x => x.Bookings)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (s == null) return NotFound();
            if (s.Status == SessionStatus.Cancelled)
            {
                TempData["Msg"] = "Session is already cancelled.";
                return RedirectToAction(nameof(Details), new { id });
            }

            s.Status = SessionStatus.Cancelled;

            var admin = await _userManager.GetUserAsync(User);
            foreach (var b in s.Bookings.Where(b => b.Status == BookingStatus.Booked))
            {
                b.Status = BookingStatus.Cancelled;
                b.CancelledUtc = DateTime.UtcNow;
                b.CancelledByUserId = admin?.Id;
            }

            await _db.SaveChangesAsync();
            // TODO: send notifications/emails here
            TempData["Msg"] = "Session cancelled and attendees notified.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Admin/Sessions/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var s = await _db.TrainingSessions
                .Include(x => x.Bookings)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (s == null) return NotFound();
            if (s.Status == SessionStatus.Completed)
            {
                TempData["Msg"] = "Session already marked completed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            s.Status = SessionStatus.Completed;

            foreach (var b in s.Bookings.Where(b => b.Status == BookingStatus.Booked))
            {
                b.Status = BookingStatus.Completed;
            }

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Session marked completed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Admin/Sessions/Duplicate/5 (nice-to-have)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duplicate(int id)
        {
            var s = await _db.TrainingSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();

            var copy = new TrainingSession
            {
                Title = s.Title,
                Level = s.Level,
                StartUtc = s.StartUtc.AddDays(7), // next week by default
                EndUtc = s.EndUtc.AddDays(7),
                Location = s.Location,
                Capacity = s.Capacity,
                Price = s.Price,
                TrainingProgramId = s.TrainingProgramId,
                CoachId = s.CoachId,
                Status = SessionStatus.Scheduled,
                ImageUrl = s.ImageUrl,
                ImageAlt = s.ImageAlt
            };

            _db.TrainingSessions.Add(copy);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Session duplicated to next week.";
            return RedirectToAction(nameof(Edit), new { id = copy.Id });
        }

        // -------- helpers --------

        private async Task PopulateProgramsAsync()
        {
            var programs = await _db.TrainingPrograms
                .Where(p => p.IsActive) // <-- only active
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            ViewBag.Programs = new SelectList(programs, "Id", "Name");
        }


        private static DateTime ToUtc(DateTime local)
        {
            // Treat incoming as local time; convert to UTC
            return DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();
        }

        private static DateTime FromUtcToLocal(DateTime utc)
        {
            return DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
        }

        private async Task<(bool ok, string? url, string? error)> SaveImageAsync(IFormFile file)
        {
            // Basic validations
            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return (false, null, "Only .jpg, .jpeg, .png allowed.");

            if (!file.ContentType.StartsWith("image/"))
                return (false, null, "Invalid content type.");

            if (file.Length > 3 * 1024 * 1024) // 3 MB
                return (false, null, "File too large (max 3 MB).");

            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "sessions");
            Directory.CreateDirectory(uploadsRoot);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"/uploads/sessions/{fileName}";
            return (true, url, null);
        }

        private void DeleteFileIfExists(string? urlPath)
        {
            if (string.IsNullOrWhiteSpace(urlPath)) return;
            if (!urlPath.StartsWith("/")) return;

            var physical = Path.Combine(_env.WebRootPath, urlPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physical))
            {
                try { System.IO.File.Delete(physical); } catch { /* ignore */ }
            }
        }
    }
}
 