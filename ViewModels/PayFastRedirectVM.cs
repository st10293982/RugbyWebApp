using System.Collections.Generic;

namespace PassItOnAcademy.ViewModels
{
    public class PayFastRedirectVM
    {
        public string ActionUrl { get; set; } = "";
        public Dictionary<string, string> Fields { get; set; } = new();
    }
}
 