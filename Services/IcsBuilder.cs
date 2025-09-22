// Services/IcsBuilder.cs
using System.Text;
using PassItOnAcademy.Models;

namespace PassItOnAcademy.Services
{
    public static class IcsBuilder
    {
        public static (string fileName, string contentType, byte[] content) BuildSessionInvite(Booking b)
        {
            // Guard a bit
            if (b.Session == null)
                throw new InvalidOperationException("Booking.Session is required to build an ICS.");

            var startUtc = b.Session.StartUtc;
            // Works whether EndUtc is DateTime or DateTime?
            var endUtc = (b.Session.EndUtc is DateTime e ? e : startUtc.AddHours(1));

            var dtStart = startUtc.ToString("yyyyMMdd'T'HHmmss'Z'");
            var dtEnd = endUtc.ToString("yyyyMMdd'T'HHmmss'Z'");
            var uid = $"session-{b.SessionId}-booking-{b.Id}@passitonacademy";

            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//PassItOn Academy//EN");
            sb.AppendLine("METHOD:REQUEST");
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{uid}");
            sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}");
            sb.AppendLine($"DTSTART:{dtStart}");
            sb.AppendLine($"DTEND:{dtEnd}");
            sb.AppendLine($"SUMMARY:{Escape(b.Session.Title)}");
            sb.AppendLine($"LOCATION:{Escape(b.Session.Location)}");
            sb.AppendLine("END:VEVENT");
            sb.AppendLine("END:VCALENDAR");

            return ($"session-{b.SessionId}.ics", "text/calendar", Encoding.UTF8.GetBytes(sb.ToString()));

            static string Escape(string? s) => (s ?? "").Replace(",", "\\,").Replace(";", "\\;").Replace("\n", "\\n");
        }
    }
}
