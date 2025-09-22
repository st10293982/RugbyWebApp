using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PassItOnAcademy.Models
{
    public class TrainingSession
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Title { get; set; } = default!;

        [MaxLength(40)]
        public string? Level { get; set; }
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }

        [MaxLength(160)]
        public string? Location { get; set; }
        public int Capacity { get; set; } = 1;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
        public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

        // Optional program
        public int? TrainingProgramId { get; set; }
        public TrainingProgram? TrainingProgram { get; set; }

   
        [MaxLength(512)]
        public string? ImageUrl { get; set; }     // e.g., "/uploads/sessions/abc123.jpg"

        [MaxLength(140)]
        public string? ImageAlt { get; set; }     // accessible alt text ("Scrum clinic U18")

        // Coach
        [Required]
        public string CoachId { get; set; } = default!;
        public ApplicationUser Coach { get; set; } = default!;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
        public ICollection<TrainingMaterial> Materials { get; set; } = new List<TrainingMaterial>();
    }

}
