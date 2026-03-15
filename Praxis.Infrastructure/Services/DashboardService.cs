using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly PraxisDbContext _db;

    public DashboardService(PraxisDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardStats> GetStatsAsync()
    {
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);

        var totalInvoices = await _db.Invoices.ToListAsync();
        var currentMonthInvoices = await _db.Invoices
            .Where(i => i.InvoiceDate >= startOfMonth && i.InvoiceDate < endOfMonth)
            .ToListAsync();

        var stats = new DashboardStats
        {
            TotalPatients = await _db.Patients.CountAsync(),
            TotalAppointments = await _db.Appointments.CountAsync(),
            TotalInvoices = totalInvoices.Count,
            TotalPrescriptions = await _db.Prescriptions.CountAsync(),
            TotalRevenue = totalInvoices.Sum(i => i.TotalAmount),

            CurrentMonthAppointments = await _db.Appointments
                .CountAsync(a => a.StartTime >= startOfMonth && a.StartTime < endOfMonth),

            CurrentMonthInvoices = currentMonthInvoices.Count,
            CurrentMonthRevenue = currentMonthInvoices.Sum(i => i.TotalAmount)
        };

        return stats;
    }
}