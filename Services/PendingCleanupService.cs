// Services/PendingCleanupService.cs
using Microsoft.EntityFrameworkCore;
using PassItOnAcademy.Data;
using PassItOnAcademy.Models;

namespace PassItOnAcademy.Services
{
    public class PendingCleanupService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly TimeSpan _ttl = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _safety = TimeSpan.FromMinutes(2);
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

        public PendingCleanupService(IServiceProvider sp) => _sp = sp;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var now = DateTime.UtcNow;
                    var cutoff = now - _ttl;
                    var safetyCutoff = now - _safety;

                    // Only auto-cancel online (PayFast) pending, and skip very-new ones (safety buffer)
                    var stale = await db.Payments
                        .Include(p => p.Booking).ThenInclude(b => b.Session)
                        .Where(p =>
                            p.Method == PaymentMethod.PayFast &&
                            p.Status == PaymentStatus.Pending &&
                            p.CreatedUtc <= cutoff &&
                            p.CreatedUtc <= safetyCutoff &&
                            p.Booking.Status == BookingStatus.Pending)
                        .ToListAsync(stoppingToken);

                    foreach (var p in stale)
                    {
                        p.Status = PaymentStatus.Cancelled;
                        p.UpdatedUtc = now;
                        if (p.Booking.Status == BookingStatus.Pending)
                        {
                            p.Booking.Status = BookingStatus.Cancelled;
                            p.Booking.CancelledUtc = now;
                        }
                    }

                    if (stale.Count > 0)
                        await db.SaveChangesAsync(stoppingToken);
                }
                catch
                {
                    // TODO: inject ILogger<PendingCleanupService> and log
                }

                try { await Task.Delay(_interval, stoppingToken); }
                catch (TaskCanceledException) { }
            }
        }
    }
}
