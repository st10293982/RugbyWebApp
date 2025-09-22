using System;

namespace PassItOnAcademy.ViewModels
{
    public class SessionListItemVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string? Level { get; set; }
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public string? Location { get; set; }
        public int Capacity { get; set; }
        public int BookedCount { get; set; }
        public int Remaining => Math.Max(0, Capacity - BookedCount);
        public decimal Price { get; set; }
        public string Status { get; set; } = default!;
        public string? ImageUrl { get; set; }
    }
}
