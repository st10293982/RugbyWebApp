using System.ComponentModel.DataAnnotations;

namespace PassItOnAcademy.Models
{
    public enum ContactStatus
    {
        New = 0,
        Read = 1,
        Archived = 2
    }

    public class ContactMessage
    {
        public int Id { get; set; }

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

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public ContactStatus Status { get; set; } = ContactStatus.New;
    }
}
