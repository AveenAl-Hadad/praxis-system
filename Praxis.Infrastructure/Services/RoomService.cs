using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class RoomService : IRoomService
{
    private readonly PraxisDbContext _context;

    public RoomService(PraxisDbContext context)
    {
        _context = context;
    }

    public async Task<List<Room>> GetAllAsync()
    {
        return await _context.Rooms
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<List<Room>> GetActiveAsync()
    {
        return await _context.Rooms
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<Room?> GetByIdAsync(int id)
    {
        return await _context.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task AddAsync(Room room)
    {
        Validate(room);

        var exists = await _context.Rooms.AnyAsync(r => r.Name == room.Name);
        if (exists)
            throw new InvalidOperationException("Ein Raum mit diesem Namen existiert bereits.");

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Room room)
    {
        Validate(room);

        var existing = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == room.Id);
        if (existing == null)
            throw new InvalidOperationException("Raum wurde nicht gefunden.");

        var duplicate = await _context.Rooms.AnyAsync(r => r.Name == room.Name && r.Id != room.Id);
        if (duplicate)
            throw new InvalidOperationException("Ein anderer Raum mit diesem Namen existiert bereits.");

        existing.Name = room.Name.Trim();
        existing.Beschreibung = room.Beschreibung?.Trim() ?? string.Empty;
        existing.IsActive = room.IsActive;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id);
        if (existing == null)
            throw new InvalidOperationException("Raum wurde nicht gefunden.");

        _context.Rooms.Remove(existing);
        await _context.SaveChangesAsync();
    }

    private static void Validate(Room room)
    {
        if (string.IsNullOrWhiteSpace(room.Name))
            throw new ArgumentException("Raumname darf nicht leer sein.");

        room.Name = room.Name.Trim();
        room.Beschreibung = room.Beschreibung?.Trim() ?? string.Empty;
    }
}