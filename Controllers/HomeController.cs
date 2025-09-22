using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PassItOnAcademy.Data;
using PassItOnAcademy.Models;
using PassItOnAcademy.ViewModels;

namespace PassItOnAcademy.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var nowUtc = DateTime.UtcNow;

            var upcoming = await _db.TrainingSessions
                .Include(s => s.Bookings)
                .Where(s => s.Status == SessionStatus.Scheduled && s.StartUtc > nowUtc)
                .OrderBy(s => s.StartUtc)
                .Take(6)
                .Select(s => new HomeIndexViewModel.SessionCard
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
                    ImageAlt = s.ImageAlt
                })
                .ToListAsync();

            var vm = new HomeIndexViewModel { Upcoming = upcoming };
            return View(vm);
        }

        public IActionResult About() => View();

        public IActionResult Programs()
        {
            var items = new List<ProgramCardVM>
            {
                new("Backline Skills", "Explosive speed, handling under pressure, defensive reads.", 650m, Url.Content("~/images/childtraining2.jpg"), "Backline"),
                new("Fly-Half Mastery", "Decision making, kicking options, game control.", 700m, Url.Content("~/images/childtraining3.jpg"), "Fly-Half"),
                new("Forward Power", "Breakdown dominance, scrummaging foundations, contact confidence.", 650m, Url.Content("~/images/childtraining4.jpg"), "Forwards")
            };
            return View(items);
        }

        // ---------- CONTACT ----------
        [HttpGet]
        public IActionResult Contact()
        {
            return View(new ContactFormVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactFormVM vm)
        {
            // honeypot: if bots fill this hidden field, silently succeed
            if (!string.IsNullOrWhiteSpace(vm.Company))
                return RedirectToAction(nameof(ContactThanks));

            if (!ModelState.IsValid)
                return View(vm);

            var msg = new ContactMessage
            {
                Name = vm.Name.Trim(),
                Email = vm.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(vm.Phone) ? null : vm.Phone.Trim(),
                Subject = vm.Subject.Trim(),
                Message = vm.Message.Trim(),
                CreatedUtc = DateTime.UtcNow,
                Status = ContactStatus.New
            };

            _db.ContactMessages.Add(msg);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(ContactThanks));
        }

        [HttpGet]
        public IActionResult ContactThanks() => View();

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }

    public record ProgramCardVM(string Title, string Blurb, decimal Price, string ImageUrl, string Tag);
}
