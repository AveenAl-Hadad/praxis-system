using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class ReportsService : IReportsService
{
    private readonly PraxisDbContext _db;

    public ReportsService(PraxisDbContext db)
    {
        _db = db;
    }

    public async Task<PracticeReportSummary> GetSummaryAsync(DateTime from, DateTime to)
    {
        return new PracticeReportSummary
        {
            PatientCount = await _db.Patients.CountAsync(p => p.IsActive),

            AppointmentCount = await _db.Appointments
                .CountAsync(a => a.StartTime >= from && a.StartTime <= to),

            DiagnosisCount = await _db.PatientDiagnoses
                .CountAsync(d => d.DiagnosedAt >= from && d.DiagnosedAt <= to),

            InvoiceCount = await _db.Invoices
                .CountAsync(i => i.InvoiceDate >= from && i.InvoiceDate <= to),

            Revenue = (await _db.Invoices
                                        .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to)
                                        .Select(i => i.TotalAmount)
                                        .ToListAsync())
                                        .Sum()
        };
    }

    public async Task<List<ReportRow>> GetDiagnosisStatsAsync(DateTime from, DateTime to)
    {
        return await _db.PatientDiagnoses
            .Include(d => d.CatalogItem)
            .Where(d => d.DiagnosedAt >= from && d.DiagnosedAt <= to)
            .GroupBy(d => d.CatalogItem.Name)
            .Select(g => new ReportRow
            {
                Name = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();
    }

    public async Task<List<ReportRow>> GetInvoiceStatsAsync(DateTime from, DateTime to)
    {
        to = to.Date.AddDays(1).AddTicks(-1);

        var invoices = await _db.Invoices
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to)
            .ToListAsync();

        return invoices
            .GroupBy(i => i.InvoiceDate.Date)
            .Select(g => new ReportRow
            {
                Name = g.Key.ToString("dd.MM.yyyy"),
                Count = g.Count(),
                Amount = g.Sum(x => x.TotalAmount)
            })
            .OrderBy(x => x.Name)
            .ToList();
    }
    public async Task<List<ReportRow>> GetAppointmentStatsAsync(DateTime from, DateTime to)
    {
        return await _db.Appointments
            .Where(a => a.StartTime >= from && a.StartTime <= to)
            .GroupBy(a => a.Status)
            .Select(g => new ReportRow
            {
                Name = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();
    }
}