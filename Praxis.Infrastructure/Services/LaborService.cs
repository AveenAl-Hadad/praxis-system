using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;

namespace Praxis.Infrastructure.Services;
    public class LaborService : ILaborService
    {
        private readonly PraxisDbContext _context;

        public LaborService(PraxisDbContext context)
        {
            _context = context;
        }

        public async Task<List<LaborRecord>> GetAllAsync()
        {
            return await _context.LaborRecords.ToListAsync();
        }
    public async Task<LaborRecord?> GetByIdAsync(int laborId)
    {
        return await _context.LaborRecords
            .Include(x => x.Patient)
            .FirstOrDefaultAsync(x => x.Id == laborId);
    }
    public async Task AddAsync(LaborRecord record)
        {
            _context.LaborRecords.Add(record);
            await _context.SaveChangesAsync();
        }
    public async Task AssignToPatientAsync(int laborId, int patientId)
    {
        var item = await _context.LaborRecords.FindAsync(laborId);
        if (item == null) return;

        item.PatientId = patientId;
        item.Status = "Zugeordnet";

        await _context.SaveChangesAsync();
    }

    public async Task SetStatusAsync(int laborId, string status)
    {
        var item = await _context.LaborRecords.FindAsync(laborId);
        if (item == null) return;

        item.Status = status;

        await _context.SaveChangesAsync();
    }
    public async Task MarkAddedToMedicalRecordAsync(int laborId)
    {
        var item = await _context.LaborRecords.FindAsync(laborId);

        if (item == null)
            return;

        item.Status = "In Karteikarte";

        await _context.SaveChangesAsync();
    }
}
