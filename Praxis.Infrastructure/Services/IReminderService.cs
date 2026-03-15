using Praxis.Domain.Entities;

namespace Praxis.Infrastructure.Services;

public interface IReminderService
{
    Task SendAppointmentReminderAsync(Appointment appointment);
}