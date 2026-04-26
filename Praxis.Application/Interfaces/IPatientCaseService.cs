using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface IPatientCaseService
{
    Task<List<PatientCase>> GetByPatientAsync(int patientId);
    Task<PatientCase?> GetActiveCaseAsync(int patientId);
    Task<PatientCase> CreateAsync(PatientCase patientCase);
    Task CloseAsync(int caseId);
}