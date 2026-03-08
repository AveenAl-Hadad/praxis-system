using Praxis.Domain.Entities;

namespace Praxis.Infrastructure.Services.Interface;

public interface IPatientService
{
    Task<IEnumerable<Patient>> GetAllPatientsAsync();
    Task AddPatientAsync(Patient patient);
    Task UpdatePatientAsync(Patient patient);
    Task DeletePatientAsync(int id);
    Task ToggleActiveAsync(int id);
}