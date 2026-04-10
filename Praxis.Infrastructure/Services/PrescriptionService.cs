using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

/// <summary>
/// Service zur Verwaltung von Rezepten.
/// Enthält CRUD-Operationen sowie Audit-Logging.
/// </summary>
public class PrescriptionService : IPrescriptionService
{
    private readonly PraxisDbContext _db;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Konstruktor mit Dependency Injection für DbContext und AuditService.
    /// </summary>
    public PrescriptionService(PraxisDbContext db, IAuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    /// <summary>
    /// Gibt alle Rezepte zurück (neueste zuerst).
    /// </summary>
    public async Task<List<Prescription>> GetAllPrescriptionsAsync()
    {
        return await _db.Prescriptions
            .AsNoTracking() // Performance bei Read-Only
            .Include(p => p.Patient)
            .OrderByDescending(p => p.IssueDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gibt alle Rezepte eines bestimmten Patienten zurück.
    /// </summary>
    public async Task<List<Prescription>> GetPrescriptionsByPatientAsync(int patientId)
    {
        return await _db.Prescriptions
            .AsNoTracking()
            .Include(p => p.Patient)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.IssueDate)
            .ToListAsync();
    }

    /// <summary>
    /// Holt ein Rezept anhand der ID.
    /// </summary>
    public async Task<Prescription?> GetPrescriptionByIdAsync(int id)
    {
        return await _db.Prescriptions
            .AsNoTracking()
            .Include(p => p.Patient)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// Fügt ein neues Rezept hinzu.
    /// </summary>
    public async Task AddPrescriptionAsync(Prescription prescription, string userName)
    {
        // 🔒 Validierung
        if (prescription == null)
            throw new ArgumentNullException(nameof(prescription));

        if (prescription.PatientId <= 0)
            throw new ArgumentException("Ungültiger Patient.");

        // 🧾 Automatische Rezeptnummer, falls nicht gesetzt
        if (string.IsNullOrWhiteSpace(prescription.PrescriptionNumber))
        {
            prescription.PrescriptionNumber =
                $"RX-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        }

        // Falls kein Datum gesetzt → jetzt
        if (prescription.IssueDate == default)
        {
            prescription.IssueDate = DateTime.UtcNow;
        }

        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();

        // 📝 Audit-Log
        await _auditService.LogAsync(
            userName,
            "CREATE",
            "Prescription",
            $"Rezept {prescription.PrescriptionNumber} für Patient {prescription.PatientId} erstellt");
    }

    /// <summary>
    /// Löscht ein Rezept.
    /// </summary>
    public async Task DeletePrescriptionAsync(int id, string userName)
    {
        var prescription = await _db.Prescriptions.FindAsync(id);

        if (prescription != null)
        {
            _db.Prescriptions.Remove(prescription);
            await _db.SaveChangesAsync();

            // 📝 Audit-Log
            await _auditService.LogAsync(
                userName,
                "DELETE",
                "Prescription",
                $"Rezept {prescription.PrescriptionNumber} gelöscht");
        }
    }
}