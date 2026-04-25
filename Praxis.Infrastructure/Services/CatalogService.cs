using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class CatalogService : ICatalogService
{
    private readonly PraxisDbContext _context;

    public CatalogService(PraxisDbContext context)
    {
        _context = context;
    }

    public async Task<List<CatalogItem>> GetAllAsync()
    {
        return await _context.CatalogItems
            .AsNoTracking()
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Code)
            .ToListAsync();
    }

    public async Task<List<CatalogItem>> GetByCategoryAsync(string category)
    {
        return await _context.CatalogItems
            .AsNoTracking()
            .Where(x => x.Category == category && x.IsActive)
            .OrderBy(x => x.Code)
            .ToListAsync();
    }

    public async Task AddAsync(CatalogItem item)
    {
        Validate(item);

        bool exists = await _context.CatalogItems.AnyAsync(x =>
            x.Category == item.Category && x.Code == item.Code);

        if (exists)
            throw new InvalidOperationException("Dieser Code existiert in dieser Kategorie bereits.");

        _context.CatalogItems.Add(item);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CatalogItem item)
    {
        Validate(item);

        var existing = await _context.CatalogItems.FirstOrDefaultAsync(x => x.Id == item.Id);

        if (existing == null)
            throw new InvalidOperationException("Katalogeintrag wurde nicht gefunden.");

        bool duplicate = await _context.CatalogItems.AnyAsync(x =>
            x.Id != item.Id &&
            x.Category == item.Category &&
            x.Code == item.Code);

        if (duplicate)
            throw new InvalidOperationException("Dieser Code existiert in dieser Kategorie bereits.");

        existing.Category = item.Category.Trim();
        existing.Code = item.Code.Trim();
        existing.Name = item.Name.Trim();
        existing.Description = item.Description?.Trim() ?? string.Empty;
        existing.IsActive = item.IsActive;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await _context.CatalogItems.FirstOrDefaultAsync(x => x.Id == id);

        if (existing == null)
            throw new InvalidOperationException("Katalogeintrag wurde nicht gefunden.");

        _context.CatalogItems.Remove(existing);
        await _context.SaveChangesAsync();
    }

    private static void Validate(CatalogItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Category))
            throw new ArgumentException("Kategorie darf nicht leer sein.");

        if (string.IsNullOrWhiteSpace(item.Code))
            throw new ArgumentException("Code darf nicht leer sein.");

        if (string.IsNullOrWhiteSpace(item.Name))
            throw new ArgumentException("Name darf nicht leer sein.");

        item.Category = item.Category.Trim();
        item.Code = item.Code.Trim();
        item.Name = item.Name.Trim();
        item.Description = item.Description?.Trim() ?? string.Empty;
    }
}