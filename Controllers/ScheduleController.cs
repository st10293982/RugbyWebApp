using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PassItOnAcademy.Data;
using PassItOnAcademy.Models;
using PassItOnAcademy.ViewModels;

namespace PassItOnAcademy.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ScheduleController(ApplicationDbContext db) => _db = db;

        // GET: /Schedule
        public async Task<IActionResult> Index(
            DateTime? from, DateTime? to, string? level, int? programId, string? q, int page = 1)
        {
            var nowUtc = DateTime.UtcNow;

            var query = _db.TrainingSessions
                .AsNoTracking()
                .Include(s => s.Bookings)
                .Include(s => s.TrainingProgram)
                .Where(s => s.Status == SessionStatus.Scheduled && s.StartUtc > nowUtc);

            if (from is not null) query = query.Where(s => s.StartUtc >= DateTime.SpecifyKind(from.Value, DateTimeKind.Local).ToUniversalTime());
            if (to is not null) query = query.Where(s => s.StartUtc < DateTime.SpecifyKind(to.Value, DateTimeKind.Local).ToUniversalTime());
            if (!string.IsNullOrWhiteSpace(level)) query = query.Where(s => s.Level == level);
            if (programId is not null) query = query.Where(s => s.TrainingProgramId == programId);
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(s => s.Title.Contains(q));

            const int pageSize = 9;
            var total = await query.CountAsync();

            var items = await query
                .OrderBy(s => s.StartUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new ScheduleIndexVM.Item
                {
                    Id = s.Id,
                    Title = s.Title,
                    Level = s.Level,
                    StartLocal = DateTime.SpecifyKind(s.StartUtc, DateTimeKind.Utc).ToLocalTime(),
                    Location = s.Location,
                    Price = s.Price,
                    Capacity = s.Capacity,
                    Booked = s.Bookings.Count(b => b.Status == BookingStatus.Booked),
                    ImageUrl = s.ImageUrl,
                    ImageAlt = s.ImageAlt,
                    ProgramName = s.TrainingProgram != null ? s.TrainingProgram.Name : null
                })
                .ToListAsync();

            // filter options
            var levels = await _db.TrainingSessions
                .Where(s => s.Level != null)
                .Select(s => s.Level!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var programs = await _db.TrainingPrograms
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            var vm = new ScheduleIndexVM
            {
                Filters = new ScheduleFiltersVM
                {
                    From = from,
                    To = to,
                    Level = level,
                    ProgramId = programId,
                    Q = q,
                    LevelOptions = new SelectList(levels),
                    ProgramOptions = new SelectList(programs, "Id", "Name")
                },

                Items = items,
                Pagination = new ScheduleIndexVM.Pager
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = total
                }
            };

            return View(vm); // Views/Schedule/Index.cshtml
        }

        // GET: /Schedule/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var s = await _db.TrainingSessions
                .AsNoTracking()
                .Include(x => x.TrainingProgram)
                .Include(x => x.Coach)
                .FirstOrDefaultAsync(x => x.Id == id && x.Status != SessionStatus.Cancelled);

            if (s == null) return NotFound();

            var booked = await _db.Bookings.CountAsync(b => b.SessionId == id && b.Status == BookingStatus.Booked);
            var vm = new SessionDetailsVM
            {
                Id = s.Id,
                Title = s.Title,
                Level = s.Level,
                StartLocal = DateTime.SpecifyKind(s.StartUtc, DateTimeKind.Utc).ToLocalTime(),
                EndLocal = DateTime.SpecifyKind(s.EndUtc, DateTimeKind.Utc).ToLocalTime(),
                Location = s.Location,
                Price = s.Price,
                Capacity = s.Capacity,
                Booked = booked,
                ImageUrl = s.ImageUrl,
                ImageAlt = s.ImageAlt,
                ProgramName = s.TrainingProgram?.Name,
                ProgramDescription = s.TrainingProgram?.Description,
                CoachName = s.Coach?.FullName
            };

            return View(vm); // Views/Schedule/Details.cshtml
        }
    }
}

