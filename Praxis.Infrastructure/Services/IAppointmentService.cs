using Praxis.Domain.Entities;

namespace Praxis.Infrastructure.Services;

public interface IAppointmentService
{
    Task AddAppointmentAsync(Appointment appointment);
    Task<List<Appointment>> GetAllAppointmentsAsync();
    Task<List<Appointment>> GetAppointmentsByDateAsync(DateTime date);
    Task<Appointment?> GetAppointmentByIdAsync(int id);
    Task UpdateAppointmentAsync(Appointment appointment);
    Task DeleteAppointmentAsync(int id);
}