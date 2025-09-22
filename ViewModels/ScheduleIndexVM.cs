using System;
using System.Collections.Generic;

namespace PassItOnAcademy.ViewModels
{
    public class ScheduleIndexVM
    {
        public ScheduleFiltersVM Filters { get; set; } = new();
        public List<Item> Items { get; set; } = new();
        public Pager Pagination { get; set; } = new();

        public class Item
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string? Level { get; set; }
            public DateTime StartLocal { get; set; }
            public string? Location { get; set; }
            public decimal Price { get; set; }
            public int Capacity { get; set; }
            public int Booked { get; set; }
            public string? ImageUrl { get; set; }
            public string? ImageAlt { get; set; }
            public string? ProgramName { get; set; }
            public int SpotsLeft => Math.Max(0, Capacity - Booked);
        }

        public class Pager
        {
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int Total { get; set; }
            public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
        }
    }
}
