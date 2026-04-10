using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces
{ 
public interface IPatientService
{
    Task<IEnumerable<Patient>> GetAllPatientsAsync();
    Task AddPatientAsync(Patient patient, string userName);
    Task UpdatePatientAsync(Patient patient);
    Task DeletePatientAsync(int id, string userName);
    Task ToggleActiveAsync(int id);
}
}