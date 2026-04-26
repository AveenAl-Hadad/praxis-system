namespace Praxis.Domain.Entities;

public class DashboardStats
{
    public int TotalPatients { get; set; }
    public int TotalAppointments { get; set; }
    public int TotalInvoices { get; set; }
    public int TotalPrescriptions { get; set; }
    public decimal TotalRevenue { get; set; }
    public int CurrentMonthAppointments { get; set; }
    public int CurrentMonthInvoices { get; set; }
    public decimal CurrentMonthRevenue { get; set; }

    public int TodayAppointments { get; set; }
    public int TodayCheckedIn { get; set; }
    public int TodayInTreatment { get; set; }
    public int TodayCompleted { get; set; }
    public int TodayCancelled { get; set; }

    public int OpenInvoices { get; set; }
    public int TodayServices { get; set; }
    public int TodayDiagnoses { get; set; }
    public decimal TodayRevenue { get; set; }
}