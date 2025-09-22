using System.ComponentModel.DataAnnotations;

namespace PassItOnAcademy.ViewModels
{
    public class ContactFormVM
    {
        [Required, StringLength(120)]
        public string Name { get; set; } = "";

        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = "";

        [StringLength(40)]
        public string? Phone { get; set; }

        [Required, StringLength(140)]
        public string Subject { get; set; } = "";

        [Required, StringLength(4000)]
        public string Message { get; set; } = "";

        // simple honeypot to reduce bot spam
        public string? Company { get; set; }
    }
}

