using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Exceptions;
using Microsoft.Data.Sqlite;

namespace Praxis.Infrastructure.Services;

public class PatientService
{
    private readonly PraxisDbContext _context;

    public PatientService(PraxisDbContext context)
    {
        _context = context;
    }

    public async Task<List<Patient>> GetAllPatientsAsync()
    {
        return await _context.Patients.AsNoTracking().ToListAsync();
    }
    public async Task AddPatientAsync(Patient patient)
    {
             await EnsureNoDuplicatesAsync(patient);
            _context.Patients.Add(patient);
        try
        {
            await ExecuteWithRetryAsync(() => _context.SaveChangesAsync());
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException se && se.SqliteErrorCode == 19)
        {
            throw new UserFriendlyException(
                "Diese E-Mail oder Telefonnummer existiert bereits.",
                ex);
        }
    }
    public async Task DeletePatientAsync(int id)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient != null)
        {
            try
            { 
            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new Exception("speichern fehlgeschlagen. Bitte prüfen Sie die Eingaben oder versuchen Sie es erneut.", ex);
            }
        }
    }
    public async Task UpdatePatientAsync(Patient patient)
    {
        
            var existing = await _context.Patients
             .FirstOrDefaultAsync(p => p.Id == patient.Id);

            if (existing == null) return;

            existing.Vorname = patient.Vorname;
            existing.Nachname = patient.Nachname;
            existing.Geburtsdatum = patient.Geburtsdatum;
            existing.Email = patient.Email;
            existing.Telefonnummer = patient.Telefonnummer;
        try
        {
            await ExecuteWithRetryAsync(() => _context.SaveChangesAsync());
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException se && se.SqliteErrorCode == 19)
        {
            throw new UserFriendlyException(
                "Update fehlgeschlagen: E-Mail oder Telefonnummer bereits vorhanden.",
                ex);
        }
    }
    public async Task<List<Patient>> SearchPatientsAsync(string searchTerm)
    {
        searchTerm = searchTerm.ToLower();

        return await _context.Patients.AsNoTracking()
            .Where(p =>
                p.Vorname.ToLower().Contains(searchTerm) ||
                p.Nachname.ToLower().Contains(searchTerm) ||
                p.Email.ToLower().Contains(searchTerm) ||
                p.Telefonnummer.ToLower().Contains(searchTerm))
            .ToListAsync();
    }
    private async Task EnsureNoDuplicatesAsync(Patient patient)
    {
        if (!string.IsNullOrWhiteSpace(patient.Email))
        {
            var emailExists = await _context.Patients
                .AsNoTracking()
                .AnyAsync(p => p.Email == patient.Email && p.Id != patient.Id);

            if (emailExists)
                throw new UserFriendlyException("Diese E-Mail ist bereits vorhanden.");
        }

        if (!string.IsNullOrWhiteSpace(patient.Telefonnummer))
        {
            var phoneExists = await _context.Patients
                .AsNoTracking()
                .AnyAsync(p => p.Telefonnummer == patient.Telefonnummer && p.Id != patient.Id);

            if (phoneExists)
                throw new UserFriendlyException("Diese Telefonnummer ist bereits vorhanden.");
        }
    }
    private static async Task ExecuteWithRetryAsync(Func<Task> action, int retries = 3, int delayMs = 200)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqliteException se &&
                                              (se.SqliteErrorCode == 5 || se.SqliteErrorCode == 6)) // BUSY/LOCKED
            {
                if (attempt >= retries)
                    throw new UserFriendlyException("Datenbank ist gerade gesperrt. Bitte erneut versuchen.", ex);

                await Task.Delay(delayMs * attempt);
            }
            catch (SqliteException se) when (se.SqliteErrorCode == 5 || se.SqliteErrorCode == 6)
            {
                if (attempt >= retries)
                    throw new UserFriendlyException("Datenbank ist gerade gesperrt. Bitte erneut versuchen.", se);

                await Task.Delay(delayMs * attempt);
            }
        }
    }

}