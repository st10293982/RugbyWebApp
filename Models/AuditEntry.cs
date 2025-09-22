using System.ComponentModel.DataAnnotations;

namespace PassItOnAcademy.Models
{
    public class AuditEntry
    {
        public int Id { get; set; }
        [Required, MaxLength(40)]
        public string Action { get; set; } = default!; // SessionCancelled, BookingMoved, BookingCancelled, BookingCompleted
        [Required, MaxLength(40)]
        public string EntityType { get; set; } = default!; // Session, Booking, Payment
        public int? EntityId { get; set; }
        [MaxLength(2000)]
        public string? DataJson { get; set; } // optional details
        [MaxLength(240)]
        public string? Reason { get; set; }

        [Required]
        public string PerformedByUserId { get; set; } = default!;
        public ApplicationUser? PerformedByUser { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}

