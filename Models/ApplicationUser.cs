using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PassItOnAcademy.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(120)]
        public string FullName { get; set; } = default!;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();         // as Customer
        public ICollection<TrainingSession> SessionsCoached { get; set; } = new List<TrainingSession>(); // as Coach
    }
}
