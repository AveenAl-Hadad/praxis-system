using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces
{
    public interface IAppointmentService
    {
        Task AddAppointmentAsync(Appointment appointment);
        Task<List<Appointment>> GetAllAppointmentsAsync();
        Task<List<Appointment>> GetAppointmentsByDateAsync(DateTime date);
        Task<List<Appointment>> GetAppointmentsByWeekAsync(DateTime startOfWeek);
        Task<List<Appointment>> GetAppointmentsByWeekAndPatientAsync(DateTime startOfWeek, int? patientId);
        Task<Appointment?> GetAppointmentByIdAsync(int id);
        Task<List<Appointment>> GetWaitingRoomAppointmentsAsync(DateTime date);
        Task UpdateAppointmentAsync(Appointment appointment);
        Task UpdateAppointmentStatusAsync(int appointmentId, string status);
        Task DeleteAppointmentAsync(int id);

        // Raumbezogene Verfügbarkeiten
        Task<List<DateTime>> GetAvailableSlotsAsync(DateTime date, int durationMinutes, string? roomName = null);
        Task<List<DateTime>> GetAvailableSlotsForEditAsync(DateTime date, int durationMinutes, string? roomName, int appointmentId);
        Task<bool> IsTimeSlotAvailableAsync(DateTime startTime, int durationMinutes, string? roomName = null, int? excludeAppointmentId = null);

        Task CheckInAsync(int appointmentId, string? note = null);
        Task MoveToRoomAsync(int appointmentId, string roomName);
        Task CompleteAppointmentAsync(int appointmentId);
        Task CancelAppointmentAsync(int appointmentId, string? note = null);

    }
}