using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface IDoctorBlockTimeService
{
    Task<List<DoctorBlockTime>> GetByDoctorAsync(int doctorId);
    Task<List<DoctorBlockTime>> GetActiveByDoctorAndRangeAsync(int doctorId, DateTime from, DateTime to);
    Task<DoctorBlockTime?> GetByIdAsync(int id);
    Task AddAsync(DoctorBlockTime blockTime);
    Task UpdateAsync(DoctorBlockTime blockTime);
    Task DeleteAsync(int id);
}