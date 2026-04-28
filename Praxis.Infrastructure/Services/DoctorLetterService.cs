using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class DoctorLetterService : IDoctorLetterService
{
    private readonly PraxisDbContext _db;

    public DoctorLetterService(PraxisDbContext db)
    {
        _db = db;
    }

    public async Task<List<DoctorLetter>> GetAllAsync()
    {
        return await _db.DoctorLetters
            .Include(x => x.Patient)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(DoctorLetter letter)
    {
        letter.CreatedAt = DateTime.Now;
        _db.DoctorLetters.Add(letter);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var letter = await _db.DoctorLetters.FindAsync(id);

        if (letter == null)
            return;

        _db.DoctorLetters.Remove(letter);
        await _db.SaveChangesAsync();
    }
}