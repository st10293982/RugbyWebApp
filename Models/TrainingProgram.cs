using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PassItOnAcademy.Data;

namespace PassItOnAcademy.Models
{
    public class TrainingProgram
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = default!;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(40)]
        public string? Level { get; set; } // e.g., U18, Beginner
        public int? DurationMinutes { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<TrainingSession> Sessions { get; set; } = new List<TrainingSession>();
    }
}
