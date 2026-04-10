using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces
{ 
public interface IReminderService
{
    Task SendAppointmentReminderAsync(Appointment appointment);
}
}