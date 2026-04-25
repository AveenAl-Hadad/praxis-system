using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface IPatientMedicationService
{
    Task<List<PatientMedication>> GetByPatientAsync(int patientId);
    Task<List<CatalogItem>> SearchMedicationAsync(string search);
    Task AddAsync(int patientId, int catalogItemId, string dosage, string notes);
    Task DeleteAsync(int medicationId);
}