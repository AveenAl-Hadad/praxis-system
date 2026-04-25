using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface IPatientDiagnosisService
{
    Task<List<PatientDiagnosis>> GetByPatientAsync(int patientId);
    Task<List<CatalogItem>> SearchIcdAsync(string search);
    Task AddAsync(int patientId, int catalogItemId, string notes);
    Task DeleteAsync(int diagnosisId);
}