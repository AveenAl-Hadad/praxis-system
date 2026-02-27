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
}