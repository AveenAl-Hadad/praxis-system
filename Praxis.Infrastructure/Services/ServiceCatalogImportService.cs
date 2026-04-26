using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class ServiceCatalogImportService : IServiceCatalogImportService
{
    private readonly PraxisDbContext _context;

    public ServiceCatalogImportService(PraxisDbContext context)
    {
        _context = context;
    }

    public async Task ImportAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Leistungen CSV nicht gefunden: " + filePath);

        var lines = await File.ReadAllLinesAsync(filePath);

        var existingCodes = await _context.CatalogItems
            .Where(x => x.Category == "Leistungen / GOÄ / EBM")
            .Select(x => x.Code)
            .ToListAsync();

        var existingSet = existingCodes.ToHashSet();

        var newItems = new List<CatalogItem>();

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(';');

            if (parts.Length < 2)
                continue;

            var code = parts[0].Trim();
            var name = parts[1].Trim();
            var description = parts.Length > 2 ? parts[2].Trim() : "";
            var price = 0m;

            if (parts.Length > 3)
            {
                decimal.TryParse(
                    parts[3].Trim(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.GetCultureInfo("de-DE"),
                    out price);
            }

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                continue;

            if (existingSet.Contains(code))
                continue;

            newItems.Add(new CatalogItem
            {
                Category = "Leistungen / GOÄ / EBM",
                Code = code,
                Name = name,
                Description = description,
                Price = price,
                IsActive = true
            });
        }

        if (newItems.Count == 0)
            return;

        _context.CatalogItems.AddRange(newItems);
        await _context.SaveChangesAsync();
    }
}