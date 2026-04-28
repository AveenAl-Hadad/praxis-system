namespace Praxis.Domain.Entities;

public class PracticeReportSummary
{
    public int PatientCount { get; set; }
    public int AppointmentCount { get; set; }
    public int DiagnosisCount { get; set; }
    public int InvoiceCount { get; set; }
    public decimal Revenue { get; set; }
}