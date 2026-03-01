using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

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
        try
        {
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex) 
        {
            throw new Exception("speichern fehlgeschlagen. Bitte prüfen Sie die Eingaben oder versuchen Sie es erneut.",ex);
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
        try
        {
            var existing = await _context.Patients
             .FirstOrDefaultAsync(p => p.Id == patient.Id);

            if (existing == null) return;

            existing.Vorname = patient.Vorname;
            existing.Nachname = patient.Nachname;
            existing.Geburtsdatum = patient.Geburtsdatum;
            existing.Email = patient.Email;
            existing.Telefonnummer = patient.Telefonnummer;

            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new Exception("speichern fehlgeschlagen. Bitte prüfen Sie die Eingaben oder versuchen Sie es erneut.", ex);
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
}