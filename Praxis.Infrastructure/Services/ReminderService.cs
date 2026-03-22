using Praxis.Domain.Entities;

namespace Praxis.Infrastructure.Services;

public class ReminderService : IReminderService
{
    // Email-Service zum Versenden von E-Mails
    private readonly IEmailService _emailService;

    // Konstruktor (Dependency Injection)
    public ReminderService(IEmailService emailService)
    {
        _emailService = emailService;
    }

    // Methode zum Versenden einer Termin-Erinnerung
    public async Task SendAppointmentReminderAsync(Appointment appointment)
    {
        // Wenn kein Patient vorhanden ist → nichts tun
        if (appointment.Patient == null)
            return;

        // Betreff der E-Mail
        var subject = "Termin Erinnerung";

        // Inhalt der E-Mail (mehrzeiliger String mit Platzhaltern)
        var body =
                    $"""
                    Sehr geehrte/r {appointment.Patient.FullName},

                    dies ist eine Erinnerung an Ihren Termin:

                    Datum: {appointment.StartTime:dd.MM.yyyy}
                    Uhrzeit: {appointment.StartTime:HH:mm}

                    Praxis System
                    """;

        // E-Mail senden an die Adresse des Patienten
        await _emailService.SendEmailAsync(
            appointment.Patient.Email,
            subject,
            body);
    }
}