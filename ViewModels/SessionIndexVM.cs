using System.Collections.Generic;

namespace PassItOnAcademy.ViewModels
{
    public class SessionIndexVM
    {
        public SessionFiltersVM Filters { get; set; } = new SessionFiltersVM();
        public List<SessionListItemVM> Items { get; set; } = new();
    }
}
