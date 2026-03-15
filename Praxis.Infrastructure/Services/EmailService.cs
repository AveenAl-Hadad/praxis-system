using System.Net;
using System.Net.Mail;

namespace Praxis.Infrastructure.Services;

public class EmailService : IEmailService
{
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        using var smtpClient = new SmtpClient("smtp.gmail.com")
        {
            Port = 587,
            EnableSsl = true,
            Credentials = new NetworkCredential(
                "deinemail@gmail.com",
                "qpco rxjg nlec aplk")
        };

        using var mail = new MailMessage
        {
            From = new MailAddress("dwinemail@gmail.com"),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        mail.To.Add(to);

        await smtpClient.SendMailAsync(mail);
    }
}