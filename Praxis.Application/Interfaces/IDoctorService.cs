using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface IDoctorService
{
    Task<List<Doctor>> GetAllAsync();
    Task<List<Doctor>> GetActiveAsync();
    Task<Doctor?> GetByIdAsync(int id);
    Task AddAsync(Doctor doctor);
    Task UpdateAsync(Doctor doctor);
    Task DeleteAsync(int id);
    Task<List<Doctor>> GetAllowedDoctorsForAppointmentTypeAsync(int appointmentTypeId);
    Task SetAllowedAppointmentTypesAsync(int doctorId, List<int> appointmentTypeIds);
    Task<List<int>> GetAppointmentTypeIdsForDoctorAsync(int doctorId);
    Task SetDoctorAppointmentTypesAsync(int doctorId, List<int> appointmentTypeIds);
    Task<bool> HasScheduleAsync(int doctorId);
    Task EnsureDefaultScheduleAsync(int doctorId);

}