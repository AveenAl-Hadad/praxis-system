using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class PatientMedicationService : IPatientMedicationService
{
    private readonly PraxisDbContext _context;

    public PatientMedicationService(PraxisDbContext context)
    {
        _context = context;
    }

    public async Task<List<PatientMedication>> GetByPatientAsync(int patientId)
    {
        return await _context.PatientMedications
            .AsNoTracking()
            .Include(x => x.CatalogItem)
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.PrescribedAt)
            .ToListAsync();
    }

    public async Task<List<CatalogItem>> SearchMedicationAsync(string search)
    {
        search = search.Trim().ToLower();

        if (search.Length < 2)
            return new List<CatalogItem>();

        return await _context.CatalogItems
            .AsNoTracking()
            .Where(x =>
                x.Category == "Medikamente" &&
                x.IsActive &&
                (
                    x.Code.ToLower().Contains(search) ||
                    x.Name.ToLower().Contains(search) ||
                    x.Description.ToLower().Contains(search)
                ))
            .OrderBy(x => x.Name)
            .Take(20)
            .ToListAsync();
    }

    public async Task AddAsync(int patientId, int catalogItemId, string dosage, string notes)
    {
        _context.PatientMedications.Add(new PatientMedication
        {
            PatientId = patientId,
            CatalogItemId = catalogItemId,
            Dosage = dosage?.Trim() ?? string.Empty,
            Notes = notes?.Trim() ?? string.Empty,
            PrescribedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int medicationId)
    {
        var medication = await _context.PatientMedications.FindAsync(medicationId);

        if (medication == null)
            return;

        _context.PatientMedications.Remove(medication);
        await _context.SaveChangesAsync();
    }
}