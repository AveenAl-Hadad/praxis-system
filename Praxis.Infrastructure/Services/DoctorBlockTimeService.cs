using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class DoctorBlockTimeService : IDoctorBlockTimeService
{
    private readonly PraxisDbContext _context;

    public DoctorBlockTimeService(PraxisDbContext context)
    {
        _context = context;
    }

    public async Task<List<DoctorBlockTime>> GetByDoctorAsync(int doctorId)
    {
        return await _context.DoctorBlockTimes
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId)
            .OrderBy(x => x.StartTime)
            .ToListAsync();
    }

    public async Task<List<DoctorBlockTime>> GetActiveByDoctorAndRangeAsync(int doctorId, DateTime from, DateTime to)
    {
        return await _context.DoctorBlockTimes
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.IsActive)
            .Where(x => x.StartTime < to && x.EndTime > from)
            .OrderBy(x => x.StartTime)
            .ToListAsync();
    }

    public async Task<DoctorBlockTime?> GetByIdAsync(int id)
    {
        return await _context.DoctorBlockTimes.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task AddAsync(DoctorBlockTime blockTime)
    {
        Validate(blockTime);

        _context.DoctorBlockTimes.Add(blockTime);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(DoctorBlockTime blockTime)
    {
        Validate(blockTime);

        var existing = await _context.DoctorBlockTimes.FirstOrDefaultAsync(x => x.Id == blockTime.Id);
        if (existing == null)
            throw new InvalidOperationException("Sperrzeit wurde nicht gefunden.");

        existing.StartTime = blockTime.StartTime;
        existing.EndTime = blockTime.EndTime;
        existing.Reason = blockTime.Reason.Trim();
        existing.IsAllDay = blockTime.IsAllDay;
        existing.IsActive = blockTime.IsActive;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await _context.DoctorBlockTimes.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null)
            throw new InvalidOperationException("Sperrzeit wurde nicht gefunden.");

        _context.DoctorBlockTimes.Remove(existing);
        await _context.SaveChangesAsync();
    }

    private static void Validate(DoctorBlockTime blockTime)
    {
        if (blockTime.DoctorId <= 0)
            throw new ArgumentException("Ungültiger Behandler.");

        if (blockTime.StartTime >= blockTime.EndTime)
            throw new ArgumentException("Die Sperrzeit ist ungültig.");

        if (string.IsNullOrWhiteSpace(blockTime.Reason))
            throw new ArgumentException("Bitte einen Grund für die Sperrzeit angeben.");

        blockTime.Reason = blockTime.Reason.Trim();
    }
}