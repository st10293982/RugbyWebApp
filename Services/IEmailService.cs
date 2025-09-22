// Services/IEmailService.cs
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace PassItOnAcademy.Services
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody, (string fileName, string contentType, byte[] content)? attachment = null);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailOptions _opt;
        public EmailService(IOptions<EmailOptions> opt) => _opt = opt.Value;

        public async Task SendAsync(string toEmail, string subject, string htmlBody, (string fileName, string contentType, byte[] content)? attachment = null)
        {
            using var msg = new MailMessage
            {
                From = new MailAddress(_opt.FromEmail, _opt.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            msg.To.Add(toEmail);

            if (attachment is { } a)
                msg.Attachments.Add(new Attachment(new MemoryStream(a.content), a.fileName, a.contentType));

            using var client = new SmtpClient(_opt.SmtpHost, _opt.SmtpPort)
            {
                EnableSsl = _opt.SmtpUseTls,
                Credentials = new NetworkCredential(_opt.SmtpUser, _opt.SmtpPass)
            };
            await client.SendMailAsync(msg);
        }
    }
}
