using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class PracticeSettingsService : IPracticeSettingsService
{
    private readonly PraxisDbContext _context;

    public PracticeSettingsService(PraxisDbContext context)
    {
        _context = context;
    }

    public async Task<PracticeSettings> GetAsync()
    {
        var settings = await _context.PracticeSettings.FirstOrDefaultAsync();

        if (settings != null)
            return settings;

        settings = new PracticeSettings();
        _context.PracticeSettings.Add(settings);
        await _context.SaveChangesAsync();

        return settings;
    }

    public async Task SaveAsync(PracticeSettings settings)
    {
        var existing = await _context.PracticeSettings.FirstOrDefaultAsync();

        if (existing == null)
        {
            _context.PracticeSettings.Add(settings);
        }
        else
        {
            existing.PracticeName = settings.PracticeName;
            existing.DoctorName = settings.DoctorName;
            existing.Street = settings.Street;
            existing.ZipCity = settings.ZipCity;
            existing.Phone = settings.Phone;
            existing.Email = settings.Email;
        }

        await _context.SaveChangesAsync();
    }
}