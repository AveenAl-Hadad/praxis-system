using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class PatientCaseService : IPatientCaseService
{
    private readonly PraxisDbContext _db;

    public PatientCaseService(PraxisDbContext db)
    {
        _db = db;
    }

    public async Task<List<PatientCase>> GetByPatientAsync(int patientId)
    {
        return await _db.PatientCases
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.ValidFrom)
            .ToListAsync();
    }

    public async Task<PatientCase?> GetActiveCaseAsync(int patientId)
    {
        return await _db.PatientCases
            .Where(x => x.PatientId == patientId && x.IsActive)
            .OrderByDescending(x => x.ValidFrom)
            .FirstOrDefaultAsync();
    }

    public async Task<PatientCase> CreateAsync(PatientCase patientCase)
    {
        if (patientCase.PatientId <= 0)
            throw new InvalidOperationException("Patient fehlt.");

        if (string.IsNullOrWhiteSpace(patientCase.Quarter))
            patientCase.Quarter = BuildCurrentQuarter();

        var oldActiveCases = await _db.PatientCases
            .Where(x => x.PatientId == patientCase.PatientId && x.IsActive)
            .ToListAsync();

        foreach (var item in oldActiveCases)
        {
            item.IsActive = false;
            item.ValidTo = DateTime.Today;
        }

        patientCase.IsActive = true;
        patientCase.ValidFrom = DateTime.Today;

        _db.PatientCases.Add(patientCase);
        await _db.SaveChangesAsync();

        return patientCase;
    }

    public async Task CloseAsync(int caseId)
    {
        var item = await _db.PatientCases.FindAsync(caseId);
        if (item == null)
            return;

        item.IsActive = false;
        item.ValidTo = DateTime.Today;

        await _db.SaveChangesAsync();
    }

    private static string BuildCurrentQuarter()
    {
        var now = DateTime.Today;
        var quarter = ((now.Month - 1) / 3) + 1;
        return $"{now.Year}-Q{quarter}";
    }
}