using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class AppointmentMedicalEntryService : IAppointmentMedicalEntryService
{
    private readonly PraxisDbContext _context;

    public AppointmentMedicalEntryService(PraxisDbContext context)
    {
        _context = context;
    }

    public async Task<List<AppointmentMedicalEntry>> GetByAppointmentAsync(int appointmentId)
    {
        return await _context.AppointmentMedicalEntries
            .AsNoTracking()
            .Include(x => x.DiagnosisCatalogItem)
            .Include(x => x.ServiceCatalogItem)
            .Where(x => x.AppointmentId == appointmentId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<CatalogItem>> SearchDiagnosisAsync(string search)
    {
        search = search.Trim().ToLower();

        if (search.Length < 2)
            return new List<CatalogItem>();

        return await _context.CatalogItems
            .AsNoTracking()
            .Where(x =>
                x.Category == "Diagnosen / ICD" &&
                x.IsActive &&
                (
                    x.Code.ToLower().Contains(search) ||
                    x.Name.ToLower().Contains(search)
                ))
            .OrderBy(x => x.Code)
            .Take(20)
            .ToListAsync();
    }

    public async Task<List<CatalogItem>> SearchServiceAsync(string search)
    {
        search = search.Trim().ToLower();

        if (search.Length < 1)
            return new List<CatalogItem>();

        return await _context.CatalogItems
            .AsNoTracking()
            .Where(x =>
                x.Category == "Leistungen / GOÄ / EBM" &&
                x.IsActive &&
                (
                    x.Code.ToLower().Contains(search) ||
                    x.Name.ToLower().Contains(search)
                ))
            .OrderBy(x => x.Code)
            .Take(20)
            .ToListAsync();
    }

    public async Task AddAsync(
        int appointmentId,
        int? diagnosisCatalogItemId,
        int? serviceCatalogItemId,
        string notes)
    {
        if (diagnosisCatalogItemId == null && serviceCatalogItemId == null)
            throw new InvalidOperationException("Bitte Diagnose oder Leistung auswählen.");

        _context.AppointmentMedicalEntries.Add(new AppointmentMedicalEntry
        {
            AppointmentId = appointmentId,
            DiagnosisCatalogItemId = diagnosisCatalogItemId,
            ServiceCatalogItemId = serviceCatalogItemId,
            Notes = notes?.Trim() ?? string.Empty,
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entry = await _context.AppointmentMedicalEntries.FindAsync(id);

        if (entry == null)
            return;

        _context.AppointmentMedicalEntries.Remove(entry);
        await _context.SaveChangesAsync();
    }
}