using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class AppointmentTypeService : IAppointmentTypeService
{
    private readonly PraxisDbContext _context;

    public AppointmentTypeService(PraxisDbContext context)
    {
        _context = context;
    }

    public async Task<List<AppointmentType>> GetAllAsync()
    {
        return await _context.AppointmentTypes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<List<AppointmentType>> GetActiveAsync()
    {
        return await _context.AppointmentTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<List<AppointmentType>> GetOnlineBookableAsync()
    {
        return await _context.AppointmentTypes
            .AsNoTracking()
            .Where(x => x.IsActive && x.AllowOnlineBooking)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<AppointmentType?> GetByIdAsync(int id)
    {
        return await _context.AppointmentTypes.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task AddAsync(AppointmentType appointmentType)
    {
        Validate(appointmentType);

        _context.AppointmentTypes.Add(appointmentType);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AppointmentType appointmentType)
    {
        Validate(appointmentType);

        var existing = await _context.AppointmentTypes.FirstOrDefaultAsync(x => x.Id == appointmentType.Id);
        if (existing == null)
            throw new InvalidOperationException("Terminart wurde nicht gefunden.");

        existing.Name = appointmentType.Name.Trim();
        existing.Description = appointmentType.Description?.Trim() ?? string.Empty;
        existing.DurationMinutes = appointmentType.DurationMinutes;
        existing.IsActive = appointmentType.IsActive;
        existing.AllowOnlineBooking = appointmentType.AllowOnlineBooking;
        existing.MinLeadHours = appointmentType.MinLeadHours;
        existing.MaxAdvanceDays = appointmentType.MaxAdvanceDays;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await _context.AppointmentTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null)
            throw new InvalidOperationException("Terminart wurde nicht gefunden.");

        _context.AppointmentTypes.Remove(existing);
        await _context.SaveChangesAsync();
    }

    private static void Validate(AppointmentType appointmentType)
    {
        if (string.IsNullOrWhiteSpace(appointmentType.Name))
            throw new ArgumentException("Name der Terminart darf nicht leer sein.");

        if (appointmentType.DurationMinutes <= 0)
            throw new ArgumentException("Die Dauer muss größer als 0 sein.");

        if (appointmentType.MinLeadHours < 0)
            throw new ArgumentException("MinLeadHours darf nicht negativ sein.");

        if (appointmentType.MaxAdvanceDays <= 0)
            throw new ArgumentException("MaxAdvanceDays muss größer als 0 sein.");

        appointmentType.Name = appointmentType.Name.Trim();
        appointmentType.Description = appointmentType.Description?.Trim() ?? string.Empty;
    }
}