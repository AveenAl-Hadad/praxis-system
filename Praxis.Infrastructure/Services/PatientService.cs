using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Exceptions;
using Microsoft.Data.Sqlite;

namespace Praxis.Infrastructure.Services;

/// <summary>
/// Service zur Verwaltung von Patienten.
/// Enthält CRUD-Operationen, Duplikatsprüfung, Suche und Audit-Logging.
/// </summary>
public class PatientService : IPatientService
{
    private readonly PraxisDbContext _context;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Konstruktor mit Dependency Injection für DbContext und AuditService.
    /// </summary>
    public PatientService(PraxisDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }


    /// <summary>
    /// Gibt alle Patienten ohne Tracking zurück (Performance-Optimierung).
    /// </summary>
    public async Task<IEnumerable<Patient>> GetAllPatientsAsync()
    {
        return await _context.Patients
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Fügt einen neuen Patienten hinzu.
    /// Prüft vorher auf doppelte E-Mail oder Telefonnummer.
    /// </summary>
    public async Task AddPatientAsync(Patient patient, string userName)
    {
        await EnsureNoDuplicatesAsync(patient);

        _context.Patients.Add(patient);

        try
        {
            // Speichern mit Retry-Mechanismus (z.B. bei DB-Locks)
            await ExecuteWithRetryAsync(() => _context.SaveChangesAsync());

            // Audit-Log schreiben
            await _auditService.LogAsync(
                userName,
                "CREATE",
                "Patient",
                $"Patient {patient.Vorname} {patient.Nachname} erstellt");
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqliteException se &&
            se.SqliteErrorCode == 19) // UNIQUE Constraint verletzt
        {
            throw new UserFriendlyException(
                "Diese E-Mail oder Telefonnummer existiert bereits.",
                ex);
        }
    }

    /// <summary>
    /// Löscht einen Patienten anhand der ID.
    /// </summary>
    public async Task DeletePatientAsync(int id, string userName)
    {
        var patient = await _context.Patients.FindAsync(id);

        if (patient != null)
        {
            try
            {
                var patientName = $"{patient.Vorname} {patient.Nachname}";

                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();

                // Audit-Log
                await _auditService.LogAsync(
                    userName,
                    "DELETE",
                    "Patient",
                    $"Patient {patientName} gelöscht");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new Exception(
                    "Speichern fehlgeschlagen. Bitte erneut versuchen.",
                    ex);
            }
        }
    }

    /// <summary>
    /// Aktualisiert einen bestehenden Patienten.
    /// </summary>
    public async Task UpdatePatientAsync(Patient patient)
    {
        var existing = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patient.Id);

        if (existing == null)
            return;

        // Felder aktualisieren
        existing.Vorname = patient.Vorname;
        existing.Nachname = patient.Nachname;
        existing.Geburtsdatum = patient.Geburtsdatum;
        existing.Email = patient.Email;
        existing.Telefonnummer = patient.Telefonnummer;
        existing.IsActive = patient.IsActive;
        existing.Adresse = patient.Adresse;
        existing.PLZ= patient.PLZ;
        existing.Ort = patient.Ort;
        existing.Versichertennummer = patient.Versichertennummer;
        existing.Geschlecht = patient.Geschlecht;
        existing.Versicherung = patient.Versicherung;

        try
        {
            await ExecuteWithRetryAsync(() => _context.SaveChangesAsync());
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqliteException se &&
            se.SqliteErrorCode == 19)
        {
            throw new UserFriendlyException(
                "Update fehlgeschlagen: E-Mail oder Telefonnummer bereits vorhanden.",
                ex);
        }
    }

    /// <summary>
    /// Sucht Patienten anhand eines Suchbegriffs (Name, Email, Telefon).
    /// </summary>
    public async Task<List<Patient>> SearchPatientsAsync(string searchTerm)
    {
        searchTerm = searchTerm.ToLower();

        return await _context.Patients
            .AsNoTracking()
            .Where(p =>
                p.Vorname.ToLower().Contains(searchTerm) ||
                p.Nachname.ToLower().Contains(searchTerm) ||
                p.Email.ToLower().Contains(searchTerm) ||
                p.Telefonnummer.ToLower().Contains(searchTerm))
            .ToListAsync();
    }

    /// <summary>
    /// Prüft, ob ein Patient mit gleicher E-Mail oder Telefonnummer existiert.
    /// </summary>
    private async Task EnsureNoDuplicatesAsync(Patient patient)
    {
        if (!string.IsNullOrWhiteSpace(patient.Email))
        {
            var emailExists = await _context.Patients
                .AsNoTracking()
                .AnyAsync(p =>
                    p.Email == patient.Email &&
                    p.Id != patient.Id);

            if (emailExists)
                throw new UserFriendlyException(
                    "Diese E-Mail ist bereits vorhanden.");
        }

        if (!string.IsNullOrWhiteSpace(patient.Telefonnummer))
        {
            var phoneExists = await _context.Patients
                .AsNoTracking()
                .AnyAsync(p =>
                    p.Telefonnummer == patient.Telefonnummer &&
                    p.Id != patient.Id);

            if (phoneExists)
                throw new UserFriendlyException(
                    "Diese Telefonnummer ist bereits vorhanden.");
        }
    }

    /// <summary>
    /// Führt eine DB-Operation mit Retry-Mechanismus aus (z.B. bei SQLite-Locks).
    /// </summary>
    private static async Task ExecuteWithRetryAsync(Func<Task> action,int retries = 3,int delayMs = 200)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is SqliteException se &&
                (se.SqliteErrorCode == 5 || se.SqliteErrorCode == 6)) // BUSY/LOCKED
            {
                if (attempt >= retries)
                    throw new UserFriendlyException(
                        "Datenbank ist gesperrt. Bitte erneut versuchen.",
                        ex);

                await Task.Delay(delayMs * attempt);
            }
            catch (SqliteException se) when (
                se.SqliteErrorCode == 5 || se.SqliteErrorCode == 6)
            {
                if (attempt >= retries)
                    throw new UserFriendlyException(
                        "Datenbank ist gesperrt. Bitte erneut versuchen.",
                        se);

                await Task.Delay(delayMs * attempt);
            }
        }
    }

    /// <summary>
    /// Aktiviert oder deaktiviert einen Patienten (Toggle).
    /// </summary>
    public async Task ToggleActiveAsync(int id)
    {
        var existing = await _context.Patients.FindAsync(id);

        if (existing == null)
            return;

        existing.IsActive = !existing.IsActive;

        await _context.SaveChangesAsync();
    }
}