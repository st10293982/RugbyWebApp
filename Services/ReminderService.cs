// Services/ReminderService.cs
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PassItOnAcademy.Data;
using PassItOnAcademy.Models;

namespace PassItOnAcademy.Services
{
    public class ReminderService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<ReminderService> _logger;

        // tweak these as you like
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _window = TimeSpan.FromMinutes(15); // +/- around 24h

        public ReminderService(IServiceProvider sp, ILogger<ReminderService> logger)
        {
            _sp = sp;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    var nowUtc = DateTime.UtcNow;

                    // sessions starting ~24h from now (± window)
                    var from = nowUtc.AddHours(24).AddMinutes(-_window.TotalMinutes);
                    var to = nowUtc.AddHours(24).AddMinutes(+_window.TotalMinutes);

                    var upcoming = await db.Bookings
                        .Include(b => b.User)
                        .Include(b => b.Session)
                        .Where(b =>
                            b.Status == BookingStatus.Booked &&
                            b.Session != null &&
                            b.Session.StartUtc >= from &&
                            b.Session.StartUtc <= to)
                        .AsNoTracking()
                        .ToListAsync(stoppingToken);

                    foreach (var b in upcoming)
                    {
                        var toEmail = b.User?.Email;
                        if (string.IsNullOrWhiteSpace(toEmail)) continue;

                        await email.SendAsync(
                            toEmail,
                            "Reminder: your session is in 24 hours",
                            $"<p>Hi {WebUtility.HtmlEncode(b.User!.FullName ?? toEmail)},</p>" +
                            $"<p>Reminder for <b>{WebUtility.HtmlEncode(b.Session!.Title)}</b> on " +
                            $"{b.Session.StartUtc.ToLocalTime():yyyy-MM-dd HH:mm} at {WebUtility.HtmlEncode(b.Session.Location)}.</p>"
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ReminderService error");
                }

                try { await Task.Delay(_interval, stoppingToken); }
                catch (TaskCanceledException) { /* shutdown */ }
            }
        }
    }
}
