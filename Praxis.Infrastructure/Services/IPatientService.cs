using Praxis.Domain.Entities;

namespace Praxis.Infrastructure.Services;

public interface IPatientService
{
    Task<IEnumerable<Patient>> GetAllPatientsAsync();
    Task AddPatientAsync(Patient patient, string userName);
    Task UpdatePatientAsync(Patient patient);
    Task DeletePatientAsync(int id, string userName);
    Task ToggleActiveAsync(int id);
}