// Services/EmailOptions.cs
namespace PassItOnAcademy.Services
{
    public sealed class EmailOptions
    {
        public string FromName { get; set; } = "PassItOn Academy";
        public string FromEmail { get; set; } = "no-reply@passitonacademy.co.za";

        // choose one transport below; here’s a simple SMTP example:
        public string SmtpHost { get; set; } = "smtp.sendgrid.net";
        public int SmtpPort { get; set; } = 587;
        public bool SmtpUseTls { get; set; } = true;
        public string SmtpUser { get; set; } = "";
        public string SmtpPass { get; set; } = "";
    }
}
