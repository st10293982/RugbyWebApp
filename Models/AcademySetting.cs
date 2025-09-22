
using System.ComponentModel.DataAnnotations;

namespace PassItOnAcademy.Models
{
    public class AcademySetting
    {
        public int Id { get; set; } = 1; // singleton

        // Business rules
        [Range(0, 168)]
        public int CancelCutoffHours { get; set; } = 12;
        [Range(0, 120)]
        public int SeatHoldMinutes { get; set; } = 15;
        [Range(1, 200)]
        public int DefaultCapacity { get; set; } = 10;
        [MaxLength(64)]
        public string Timezone { get; set; } = "Africa/Johannesburg";

        // Public profile
        [MaxLength(80)]
        public string CoachName { get; set; } = "Zak Smith";
        [MaxLength(400)]
        public string? CoachBio { get; set; }
        [MaxLength(80)]
        public string ContactEmail { get; set; } = "info@passiton.academy";
        [MaxLength(24)]
        public string? ContactPhone { get; set; }
        [MaxLength(160)]
        public string? Instagram { get; set; }
        [MaxLength(160)]
        public string? Facebook { get; set; }
        [MaxLength(160)]
        public string? LinkedIn { get; set; }
    }
}
