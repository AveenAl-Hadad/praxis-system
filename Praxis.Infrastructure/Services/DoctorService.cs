using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class DoctorService : IDoctorService
{
    private readonly PraxisDbContext _context;

    public DoctorService(PraxisDbContext context)
    {
        _context = context;
    }

    public async Task<List<Doctor>> GetAllAsync()
    {
        return await _context.Doctors
            .AsNoTracking()
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .ToListAsync();
    }

    public async Task<List<Doctor>> GetActiveAsync()
    {
        return await _context.Doctors
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .ToListAsync();
    }

    public async Task<Doctor?> GetByIdAsync(int id)
    {
        return await _context.Doctors
            .Include(d => d.Schedules)
            .Include(d => d.DoctorAppointmentTypes)
            .Include(d => d.BlockTimes)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<Doctor>> GetAllowedDoctorsForAppointmentTypeAsync(int appointmentTypeId)
    {
        return await _context.Doctors
            .AsNoTracking()
            .Where(d => d.IsActive && d.AllowOnlineBooking)
            .Where(d => d.DoctorAppointmentTypes.Any(x => x.AppointmentTypeId == appointmentTypeId))
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .ToListAsync();
    }

    public async Task SetAllowedAppointmentTypesAsync(int doctorId, List<int> appointmentTypeIds)
    {
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
        if (doctor == null)
            throw new InvalidOperationException("Behandler wurde nicht gefunden.");

        appointmentTypeIds = appointmentTypeIds
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        var existing = await _context.DoctorAppointmentTypes
            .Where(x => x.DoctorId == doctorId)
            .ToListAsync();

        _context.DoctorAppointmentTypes.RemoveRange(existing);

        var newItems = appointmentTypeIds.Select(typeId => new DoctorAppointmentType
        {
            DoctorId = doctorId,
            AppointmentTypeId = typeId
        });

        _context.DoctorAppointmentTypes.AddRange(newItems);
        await _context.SaveChangesAsync();
    }

    public async Task AddAsync(Doctor doctor)
    {
        Validate(doctor);

        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Doctor doctor)
    {
        Validate(doctor);

        var existing = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctor.Id);
        if (existing == null)
            throw new InvalidOperationException("Behandler wurde nicht gefunden.");

        existing.Title = doctor.Title.Trim();
        existing.FirstName = doctor.FirstName.Trim();
        existing.LastName = doctor.LastName.Trim();
        existing.Specialty = doctor.Specialty?.Trim() ?? string.Empty;
        existing.DefaultRoomName = doctor.DefaultRoomName?.Trim() ?? string.Empty;
        existing.IsActive = doctor.IsActive;
        existing.AllowOnlineBooking = doctor.AllowOnlineBooking;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == id);
        if (existing == null)
            throw new InvalidOperationException("Behandler wurde nicht gefunden.");

        _context.Doctors.Remove(existing);
        await _context.SaveChangesAsync();
    }

    private static void Validate(Doctor doctor)
    {
        if (string.IsNullOrWhiteSpace(doctor.FirstName))
            throw new ArgumentException("Vorname des Behandlers darf nicht leer sein.");

        if (string.IsNullOrWhiteSpace(doctor.LastName))
            throw new ArgumentException("Nachname des Behandlers darf nicht leer sein.");

        if (string.IsNullOrWhiteSpace(doctor.DefaultRoomName))
            throw new ArgumentException("Standardraum des Behandlers darf nicht leer sein.");

        doctor.Title = doctor.Title?.Trim() ?? string.Empty;
        doctor.FirstName = doctor.FirstName.Trim();
        doctor.LastName = doctor.LastName.Trim();
        doctor.Specialty = doctor.Specialty?.Trim() ?? string.Empty;
        doctor.DefaultRoomName = doctor.DefaultRoomName.Trim();
    }
    public async Task<List<int>> GetAppointmentTypeIdsForDoctorAsync(int doctorId)
    {
        return await _context.DoctorAppointmentTypes
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId)
            .Select(x => x.AppointmentTypeId)
            .ToListAsync();
    }

    public async Task SetDoctorAppointmentTypesAsync(int doctorId, List<int> appointmentTypeIds)
    {
        var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == doctorId);
        if (!doctorExists)
            throw new InvalidOperationException("Behandler wurde nicht gefunden.");

        appointmentTypeIds = appointmentTypeIds
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        var existing = await _context.DoctorAppointmentTypes
            .Where(x => x.DoctorId == doctorId)
            .ToListAsync();

        _context.DoctorAppointmentTypes.RemoveRange(existing);

        var newItems = appointmentTypeIds.Select(typeId => new DoctorAppointmentType
        {
            DoctorId = doctorId,
            AppointmentTypeId = typeId
        });

        _context.DoctorAppointmentTypes.AddRange(newItems);

        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasScheduleAsync(int doctorId)
    {
        return await _context.DoctorSchedules
            .AnyAsync(s => s.DoctorId == doctorId);
    }

    public async Task EnsureDefaultScheduleAsync(int doctorId)
    {
        var hasSchedule = await HasScheduleAsync(doctorId);
        if (hasSchedule)
            return;

        var weekdays = new[]
        {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    };

        foreach (var day in weekdays)
        {
            _context.DoctorSchedules.Add(new DoctorSchedule
            {
                DoctorId = doctorId,
                DayOfWeek = day,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(16, 0, 0),
                BreakStart = new TimeSpan(12, 0, 0),
                BreakEnd = new TimeSpan(13, 0, 0),
                IsActive = true
            });
        }

        await _context.SaveChangesAsync();
    }
}