using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface IAppointmentMedicalEntryService
{
    Task<List<AppointmentMedicalEntry>> GetByAppointmentAsync(int appointmentId);

    Task<List<CatalogItem>> SearchDiagnosisAsync(string search);
    Task<List<CatalogItem>> SearchServiceAsync(string search);

    Task AddAsync(
        int appointmentId,
        int? diagnosisCatalogItemId,
        int? serviceCatalogItemId,
        string notes);

    Task DeleteAsync(int id);
}