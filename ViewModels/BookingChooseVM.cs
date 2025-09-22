using System;

namespace PassItOnAcademy.ViewModels
{
    public class BookingChooseVM
    {
        public int SessionId { get; set; }
        public string Title { get; set; } = "";
        public string? Level { get; set; }
        public DateTime StartLocal { get; set; }
        public string? Location { get; set; }
        public string? ProgramName { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageAlt { get; set; }
    }
}
