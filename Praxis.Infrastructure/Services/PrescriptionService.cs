using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly PraxisDbContext _db;
    private readonly IAuditService _auditService;

    public PrescriptionService(PraxisDbContext db, IAuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    public async Task<List<Prescription>> GetAllPrescriptionsAsync()
    {
        return await _db.Prescriptions
            .Include(p => p.Patient)
            .OrderByDescending(p => p.IssueDate)
            .ToListAsync();
    }

    public async Task<List<Prescription>> GetPrescriptionsByPatientAsync(int patientId)
    {
        return await _db.Prescriptions
            .Include(p => p.Patient)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.IssueDate)
            .ToListAsync();
    }

    public async Task<Prescription?> GetPrescriptionByIdAsync(int id)
    {
        return await _db.Prescriptions
            .Include(p => p.Patient)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddPrescriptionAsync(Prescription prescription,string userName)
    {
        if (string.IsNullOrWhiteSpace(prescription.PrescriptionNumber))
        {
            prescription.PrescriptionNumber = $"RX-{DateTime.Now:yyyyMMddHHmmss}";
        }

        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();
        await _auditService.LogAsync(
        userName,
        "CREATE",
        "Prescription",
        $"Rezept für Patient {prescription.PatientId} erstellt");
    }
    public async Task DeletePrescriptionAsync(int id, string userName)
    {
        var prescription = await _db.Prescriptions.FindAsync(id);

        if (prescription != null)
        {
            _db.Prescriptions.Remove(prescription);
            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                userName,
                "DELETE",
                "Prescription",
                $"Rezept {id} gelöscht");
        }
    }
}