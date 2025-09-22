using System;

namespace PassItOnAcademy.ViewModels
{
    public class BookingConfirmVM
    {
        public int SessionId { get; set; }

        public string Title { get; set; } = "";
        public string? Level { get; set; }
        public DateTime StartLocal { get; set; }

        public string WhenDisplay { get; set; } = "";
        public string? Location { get; set; }
        public decimal Price { get; set; }

        public string? ProgramName { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageAlt { get; set; }
        public int Capacity { get; set; }
        public int Booked { get; set; }

        public int SpotsLeft => Math.Max(0, Capacity - Booked);
    }
}
