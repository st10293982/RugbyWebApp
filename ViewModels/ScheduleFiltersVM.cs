using Microsoft.AspNetCore.Mvc.Rendering;

namespace PassItOnAcademy.ViewModels
{
    public class ScheduleFiltersVM
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Level { get; set; }
        public int? ProgramId { get; set; }
        public string? Q { get; set; }

        public SelectList? LevelOptions { get; set; }
        public SelectList? ProgramOptions { get; set; }
    }
}
