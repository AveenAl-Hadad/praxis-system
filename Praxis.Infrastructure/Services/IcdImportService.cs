using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using System.Xml.Linq;

namespace Praxis.Infrastructure.Services;

public class IcdImportService : IIcdImportService
{
    private readonly PraxisDbContext _context;

    public IcdImportService(PraxisDbContext context)
    {
        _context = context;
    }

    public async Task ImportAsync(string xmlPath)
    {
        if (!File.Exists(xmlPath))
            throw new FileNotFoundException("ICD-Datei wurde nicht gefunden.", xmlPath);

        var document = XDocument.Load(xmlPath);

        var existingCodes = await _context.CatalogItems
            .Where(x => x.Category == "Diagnosen / ICD")
            .Select(x => x.Code)
            .ToListAsync();

        var existingSet = existingCodes.ToHashSet();

        var newItems = document.Descendants()
            .Where(x => x.Name.LocalName == "Class")
            .Select(cls =>
            {
                var code = cls.Attribute("code")?.Value?.Trim();

                var name = cls.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "Rubric"
                                      && x.Attribute("kind")?.Value == "preferred")
                    ?.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "Label")
                    ?.Value
                    ?.Trim();

                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                    return null;

                if (existingSet.Contains(code))
                    return null;

                return new CatalogItem
                {
                    Category = "Diagnosen / ICD",
                    Code = code,
                    Name = name,
                    Description = "ICD-10-GM",
                    IsActive = true
                };
            })
            .Where(x => x != null)
            .ToList();

        if (newItems.Count == 0)
            return;

        _context.CatalogItems.AddRange(newItems!);
        await _context.SaveChangesAsync();
    }
}