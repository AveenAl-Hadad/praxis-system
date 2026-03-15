using Praxis.Domain.Entities;

namespace Praxis.Infrastructure.Services;

public class ReminderService : IReminderService
{
    private readonly IEmailService _emailService;

    public ReminderService(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task SendAppointmentReminderAsync(Appointment appointment)
    {
        if (appointment.Patient == null)
            return;

        var subject = "Termin Erinnerung";

        var body =
                    $"""
                    Sehr geehrte/r {appointment.Patient.FullName},

                    dies ist eine Erinnerung an Ihren Termin:

                    Datum: {appointment.StartTime:dd.MM.yyyy}
                    Uhrzeit: {appointment.StartTime:HH:mm}

                    Praxis System
                    """;

        await _emailService.SendEmailAsync(
            appointment.Patient.Email,
            subject,
            body);
    }
}