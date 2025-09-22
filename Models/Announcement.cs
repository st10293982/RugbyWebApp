
using System.ComponentModel.DataAnnotations;

namespace PassItOnAcademy.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required, MaxLength(140)]
        public string Title { get; set; } = default!;

        [Required, MaxLength(4000)]
        public string Body { get; set; } = default!;

        // Global or tied to a single session
        public bool IsGlobal { get; set; }
        public int? SessionId { get; set; }
        public TrainingSession? Session { get; set; }

        // Who posted
        [Required]
        public string CreatedByUserId { get; set; } = default!;
        public ApplicationUser? CreatedByUser { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public int RecipientsNotified { get; set; } = 0; // count of emails sent
    }
}
