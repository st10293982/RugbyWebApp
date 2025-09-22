using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PassItOnAcademy.Data;
using PassItOnAcademy.Models;

namespace PassItOnAcademy.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class ExportsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ExportsController(ApplicationDbContext db) => _db = db;

        // GET: /Exports
        public IActionResult Index() => View();

        // /Exports/BookingsCsv?sessionId=&from=&to=
        public async Task<IActionResult> BookingsCsv(int? sessionId, DateTime? from, DateTime? to)
        {
            var q = _db.Bookings
                .Include(b => b.User)
                .Include(b => b.Session)
                .AsNoTracking()
                .AsQueryable();

            if (sessionId is not null) q = q.Where(b => b.SessionId == sessionId);
            if (from is not null) q = q.Where(b => b.CreatedUtc >= from.Value.ToUniversalTime());
            if (to is not null) q = q.Where(b => b.CreatedUtc < to.Value.ToUniversalTime());

            var list = await q.OrderBy(b => b.SessionId).ThenBy(b => b.Id).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("BookingId,Status,Customer,Email,Phone,SessionId,SessionTitle,StartLocal,Price");
            foreach (var b in list)
            {
                var when = b.Session?.StartUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                var price = b.Session?.Price ?? 0m;
                sb.AppendLine($"{b.Id},{b.Status},{Clean(b.User?.FullName)},{b.User?.Email},{b.User?.PhoneNumber},{b.SessionId},{Clean(b.Session?.Title)},{when},{price:0.00}");
            }
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "bookings.csv");
        }

        // /Exports/PaymentsCsv?from=&to=&status=
        public async Task<IActionResult> PaymentsCsv(DateTime? from, DateTime? to, string? status)
        {
            var q = _db.Payments
                .Include(p => p.Booking).ThenInclude(b => b.User)
                .Include(p => p.Booking).ThenInclude(b => b.Session)
                .AsNoTracking()
                .AsQueryable();

            if (from is not null) q = q.Where(p => p.CreatedUtc >= from.Value.ToUniversalTime());
            if (to is not null) q = q.Where(p => p.CreatedUtc < to.Value.ToUniversalTime());
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var st))
                q = q.Where(p => p.Status == st);

            var list = await q.OrderByDescending(p => p.CreatedUtc).ToListAsync();

            var sb = new StringBuilder();
            // Changed: Provider → Method; added GatewayRef column
            sb.AppendLine("PaymentId,Status,Amount,Currency,Method,BookingId,Customer,Email,SessionId,SessionTitle,CreatedLocal,PaidLocal,Ref,GatewayRef");
            foreach (var p in list)
            {
                var refText = p.Reference
                             ?? p.MerchantReference    // legacy fallback
                             ?? string.Empty;

                var gatewayRef = p.ProviderReference
                                ?? p.GatewayPaymentId   // legacy fallback
                                ?? string.Empty;

                var created = p.CreatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                var paid = p.PaidUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "";

                sb.AppendLine(
                    $"{p.Id},{p.Status},{p.Amount:0.00},{p.Currency},{p.Method}," +
                    $"{p.BookingId},{Clean(p.Booking?.User?.FullName)},{p.Booking?.User?.Email}," +
                    $"{p.Booking?.SessionId},{Clean(p.Booking?.Session?.Title)}," +
                    $"{created},{paid},{Clean(refText)},{Clean(gatewayRef)}"
                );
            }
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "payments.csv");
        }

        private static string Clean(string? s) =>
            string.IsNullOrWhiteSpace(s) ? "" : s.Replace(',', ';').Replace("\r", " ").Replace("\n", " ");
    }
}
