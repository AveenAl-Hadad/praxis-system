using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface IAppointmentTypeService
{
    Task<List<AppointmentType>> GetAllAsync();
    Task<List<AppointmentType>> GetActiveAsync();
    Task<List<AppointmentType>> GetOnlineBookableAsync();
    Task<AppointmentType?> GetByIdAsync(int id);
    Task AddAsync(AppointmentType appointmentType);
    Task UpdateAsync(AppointmentType appointmentType);
    Task DeleteAsync(int id);
}