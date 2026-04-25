using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class PatientDiagnosisService : IPatientDiagnosisService
{
    private readonly PraxisDbContext _context;

    public PatientDiagnosisService(PraxisDbContext context)
    {
        _context = context;
    }

    public async Task<List<PatientDiagnosis>> GetByPatientAsync(int patientId)
    {
        return await _context.PatientDiagnoses
            .AsNoTracking()
            .Include(x => x.CatalogItem)
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.DiagnosedAt)
            .ToListAsync();
    }

    public async Task<List<CatalogItem>> SearchIcdAsync(string search)
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

    public async Task AddAsync(int patientId, int catalogItemId, string notes)
    {
        bool exists = await _context.PatientDiagnoses.AnyAsync(x =>
            x.PatientId == patientId &&
            x.CatalogItemId == catalogItemId);

        if (exists)
            throw new InvalidOperationException("Diese Diagnose ist beim Patienten bereits vorhanden.");

        _context.PatientDiagnoses.Add(new PatientDiagnosis
        {
            PatientId = patientId,
            CatalogItemId = catalogItemId,
            Notes = notes?.Trim() ?? string.Empty,
            DiagnosedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int diagnosisId)
    {
        var diagnosis = await _context.PatientDiagnoses.FindAsync(diagnosisId);

        if (diagnosis == null)
            return;

        _context.PatientDiagnoses.Remove(diagnosis);
        await _context.SaveChangesAsync();
    }
}