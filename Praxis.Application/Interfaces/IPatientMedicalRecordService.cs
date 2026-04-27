using Praxis.Domain.Constants;
using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface IPatientMedicalRecordService
{
    Task<List<PatientMedicalRecordEntry>> GetByPatientAsync(int patientId);

    Task<List<PatientMedicalRecordEntry>> GetByPatientAndTypeAsync(
        int patientId,
        MedicalRecordEntryType entryType);

    Task<PatientMedicalRecordEntry> AddAsync(PatientMedicalRecordEntry entry);

    Task UpdateAsync(PatientMedicalRecordEntry entry);

    Task DeleteAsync(int id);

    Task MarkEntriesAsInvoicedAsync(List<int> entryIds, int invoiceId);

}