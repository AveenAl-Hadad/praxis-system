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
}