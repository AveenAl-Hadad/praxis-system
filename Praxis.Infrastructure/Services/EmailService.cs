using System.Net;
using System.Net.Mail;

namespace Praxis.Infrastructure.Services;

/// <summary>
/// Service zum Versenden von E-Mails über SMTP (z.B. Gmail).
/// </summary>
public class EmailService : IEmailService
{
    /// <summary>
    /// Sendet eine E-Mail asynchron.
    /// </summary>
    /// <param name="to">Empfänger-Adresse</param>
    /// <param name="subject">Betreff</param>
    /// <param name="body">Nachrichtentext</param>
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // SMTP-Client konfigurieren (hier Gmail)
        using var smtpClient = new SmtpClient("smtp.gmail.com")
        {
            Port = 587,                 // Standard-Port für TLS
            EnableSsl = true,           // Verschlüsselung aktivieren
            Credentials = new NetworkCredential(
                "deinemail@gmail.com", // Absender-Mail
                "qpco rxjg nlec aplk") // App-Passwort (kein normales Passwort!)
        };

        // E-Mail erstellen
        using var mail = new MailMessage
        {
            From = new MailAddress("dwinemail@gmail.com"), // Absender
            Subject = subject,
            Body = body,
            IsBodyHtml = false // true wenn HTML-Mail
        };

        // Empfänger hinzufügen
        mail.To.Add(to);

        // E-Mail senden
        await smtpClient.SendMailAsync(mail);
    }
}