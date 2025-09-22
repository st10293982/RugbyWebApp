namespace PassItOnAcademy.Models
{
    public class HomeIndexViewModel
    {
        public List<SessionCard> Upcoming { get; set; } = new();

        public class SessionCard
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string? Level { get; set; }
            public DateTime StartLocal { get; set; }
            public string? Location { get; set; }
            public decimal Price { get; set; }
            public int Capacity { get; set; }
            public int Booked { get; set; }

            // NEW:
            public string? ImageUrl { get; set; }
            public string? ImageAlt { get; set; }
        }
    }
}
