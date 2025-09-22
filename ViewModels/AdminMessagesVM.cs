using PassItOnAcademy.Models;

namespace PassItOnAcademy.ViewModels
{
    public class AdminMessageListItemVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string Subject { get; set; } = "";
        public string Snippet { get; set; } = "";
        public DateTime CreatedLocal { get; set; }
        public ContactStatus Status { get; set; }
    }

    public class AdminMessageIndexVM
    {
        public string? Q { get; set; }
        public ContactStatus? Status { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<AdminMessageListItemVM> Items { get; set; } = new();
    }

    public class AdminMessageDetailsVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string Subject { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime CreatedLocal { get; set; }
        public ContactStatus Status { get; set; }
    }
}
