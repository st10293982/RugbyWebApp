using System;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PassItOnAcademy.ViewModels
{
    public class SessionFiltersVM
    {
        public DateTime? FromLocal { get; set; }
        public DateTime? ToLocal { get; set; }
        public string? Level { get; set; }

        public SelectList? Levels { get; set; }
    }
}
