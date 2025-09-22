using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PassItOnAcademy.ViewModels
{
    public class TrainingSessionFormVM
    {
        public int? Id { get; set; }

        [Required, MaxLength(120)]
        public string Title { get; set; } = default!;

        [MaxLength(40)]
        public string? Level { get; set; }

        [Required, Display(Name = "Start (local time)")]
        public DateTime StartLocal { get; set; }

        [Required, Display(Name = "End (local time)")]
        public DateTime EndLocal { get; set; }

        [MaxLength(160)]
        public string? Location { get; set; }

        [Range(1, 100)]
        public int Capacity { get; set; } = 1;

        [Range(0, 100000)]
        public decimal Price { get; set; }

        [Display(Name = "Program")]
        public int? TrainingProgramId { get; set; }

        // Image
        [MaxLength(140)]
        public string? ImageAlt { get; set; }

        public IFormFile? ImageFile { get; set; }
        public string? ExistingImageUrl { get; set; }
    }
}
