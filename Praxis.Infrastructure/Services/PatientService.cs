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
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();
    }
    public async Task DeletePatientAsync(int id)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient != null)
        {
            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
        }
    }
}