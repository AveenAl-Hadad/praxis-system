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
        var invoiceAmounts = await _db.Invoices
            .Select(i => i.TotalAmount)
            .ToListAsync();

        var stats = new DashboardStats
        {
            TotalPatients = await _db.Patients.CountAsync(),
            TotalAppointments = await _db.Appointments.CountAsync(),
            TotalInvoices = await _db.Invoices.CountAsync(),
            TotalPrescriptions = await _db.Prescriptions.CountAsync(),
            TotalRevenue = invoiceAmounts.Sum()
        };

        return stats;
    }
}