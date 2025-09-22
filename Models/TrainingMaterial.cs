using System.ComponentModel.DataAnnotations;

namespace PassItOnAcademy.Models
{
    public class TrainingMaterial
    {
        public int Id { get; set; }

        [Required, MaxLength(180)]
        public string Title { get; set; } = default!;

        [MaxLength(1000)] 
        public string? Description { get; set; }

        [Required, MaxLength(512)] 
        public string FileUrl { get; set; } = default!;

        public int? SessionId { get; set; }
        public TrainingSession? Session { get; set; }

        public string? AssignedToUserId { get; set; }   // personal drills
        public ApplicationUser? AssignedToUser { get; set; }

        public string CreatedByUserId { get; set; } = default!;
        public ApplicationUser CreatedByUser { get; set; } = default!;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

    public enum SessionStatus { Scheduled, Cancelled, Completed }
    public enum BookingStatus { Pending = 0, Booked = 1, Cancelled = 2, Completed = 3 }

}