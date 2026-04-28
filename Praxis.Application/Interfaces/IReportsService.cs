using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface IReportsService
{
    Task<PracticeReportSummary> GetSummaryAsync(DateTime from, DateTime to);
    Task<List<ReportRow>> GetDiagnosisStatsAsync(DateTime from, DateTime to);
    Task<List<ReportRow>> GetInvoiceStatsAsync(DateTime from, DateTime to);
    Task<List<ReportRow>> GetAppointmentStatsAsync(DateTime from, DateTime to);
    Task<List<Patient>> GetPatientsWithoutCardAsync();
    Task<List<ReportRow>> GetServiceCodeStatsAsync(DateTime from, DateTime to);
    Task<List<ReportRow>> GetPatientStatsAsync();
}