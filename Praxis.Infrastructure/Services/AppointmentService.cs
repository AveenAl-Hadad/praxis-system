using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services.Interface;

namespace Praxis.Infrastructure.Services;

public class AppointmentService : IAppointmentService
{
    private readonly PraxisDbContext _context;

    public AppointmentService(PraxisDbContext context)
    {
        _context = context;
    }

    public async Task AddAppointmentAsync(Appointment appointment)
    {
        ValidateAppointment(appointment);
        await CheckForConflictAsync(appointment);
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Appointment>> GetAllAppointmentsAsync()
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .OrderBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<List<Appointment>> GetAppointmentsByDateAsync(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _context.Appointments
            .Include(a => a.Patient)
            .Where(a => a.StartTime >= startOfDay && a.StartTime < endOfDay)
            .OrderBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<List<Appointment>> GetAppointmentsByWeekAsync(DateTime startOfWeek)
    {
        var start = startOfWeek.Date;
        var end = start.AddDays(7);

        return await _context.Appointments
            .Include(a => a.Patient)
            .Where(a => a.StartTime >= start && a.StartTime < end)
            .OrderBy(a => a.StartTime)
            .ToListAsync();
    }
    public async Task<List<Appointment>> GetAppointmentsByWeekAndPatientAsync(DateTime startOfWeek, int? patientId)
    {
        var start = startOfWeek.Date;
        var end = start.AddDays(7);

        var query = _context.Appointments
            .Include(a => a.Patient)
            .Where(a => a.StartTime >= start && a.StartTime < end);

        if (patientId.HasValue)
        {
            query = query.Where(a => a.PatientId == patientId.Value);
        }

        return await query
            .OrderBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<Appointment?> GetAppointmentByIdAsync(int id)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Appointment>> GetWaitingRoomAppointmentsAsync(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _context.Appointments
            .Include(a => a.Patient)
            .Where(a => a.StartTime >= startOfDay && a.StartTime < endOfDay)
            .Where(a => a.Status != "Abgesagt" && a.Status != "Erledigt")
            .OrderBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task UpdateAppointmentAsync(Appointment appointment)
    {
        ValidateAppointment(appointment);
        await CheckForConflictAsync(appointment);

        var existing = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == appointment.Id);

        if (existing == null)
            throw new InvalidOperationException("Termin wurde nicht gefunden.");

        existing.PatientId = appointment.PatientId;
        existing.StartTime = appointment.StartTime;
        existing.DurationMinutes = appointment.DurationMinutes;
        existing.Reason = appointment.Reason;
        existing.Status = appointment.Status;

        await _context.SaveChangesAsync();
    }
    public async Task UpdateAppointmentStatusAsync(int appointmentId, string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status darf nicht leer sein.");

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
            throw new InvalidOperationException("Termin wurde nicht gefunden.");

        appointment.Status = status.Trim();
        await _context.SaveChangesAsync();
    }
    public async Task DeleteAppointmentAsync(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);

        if (appointment == null)
            throw new InvalidOperationException("Termin wurde nicht gefunden.");

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();
    }

    private void ValidateAppointment(Appointment appointment)
    {
        if (appointment.PatientId <= 0)
            throw new ArgumentException("Patient muss ausgewählt werden.");

        if (appointment.StartTime == default)
            throw new ArgumentException("Startzeit ist ungültig.");

        if (appointment.DurationMinutes <= 0)
            throw new ArgumentException("Dauer muss größer als 0 sein.");

        if (string.IsNullOrWhiteSpace(appointment.Reason))
            throw new ArgumentException("Grund darf nicht leer sein.");
    }

    private async Task CheckForConflictAsync(Appointment appointment)
    {
        var isAvailable = await IsTimeSlotAvailableAsync(
            appointment.StartTime,
            appointment.DurationMinutes,
            appointment.Id == 0 ? null : appointment.Id);

        if (!isAvailable)
            throw new InvalidOperationException("Es existiert bereits ein Termin in diesem Zeitraum.");
    }
    public async Task<List<DateTime>> GetAvailableSlotsAsync(DateTime date, int durationMinutes)
    {
        var availableSlots = new List<DateTime>();
        if (date.Date < DateTime.Today)
            return new List<DateTime>();

        if (durationMinutes <= 0)
            return availableSlots;

        var workingRanges = GetWorkingTimeRanges(date);
        var now = DateTime.Now;
        var isToday = date.Date == now.Date;

        foreach (var range in workingRanges)
        {
            var firstPossibleSlot = range.Start;

            if (isToday)
            {
                var nextPossibleTime = RoundUpToNext15Minutes(now);

                if (nextPossibleTime > firstPossibleSlot)
                {
                    firstPossibleSlot = nextPossibleTime;
                }
            }

            for (var slot = firstPossibleSlot; slot.AddMinutes(durationMinutes) <= range.End; slot = slot.AddMinutes(15))
            {
                var isAvailable = await IsTimeSlotAvailableAsync(slot, durationMinutes);
                if (isAvailable)
                {
                    availableSlots.Add(slot);
                }
            }
        }

        return availableSlots;
    }
    private List<(DateTime Start, DateTime End)> GetWorkingTimeRanges(DateTime date)
    {
        var ranges = new List<(DateTime Start, DateTime End)>();

        switch (date.DayOfWeek)
        {
            case DayOfWeek.Monday:
            case DayOfWeek.Tuesday:
            case DayOfWeek.Thursday:
                ranges.Add((date.Date.AddHours(8), date.Date.AddHours(12)));
                ranges.Add((date.Date.AddHours(15), date.Date.AddHours(18)));
                break;

            case DayOfWeek.Wednesday:
                ranges.Add((date.Date.AddHours(8), date.Date.AddHours(12)));
                break;

            case DayOfWeek.Friday:
                ranges.Add((date.Date.AddHours(8), date.Date.AddHours(14)));
                break;

            case DayOfWeek.Saturday:
            case DayOfWeek.Sunday:
                break;
        }

        return ranges;
    }
    private DateTime RoundUpToNext15Minutes(DateTime dateTime)
    {
        var trimmed = new DateTime(
            dateTime.Year,
            dateTime.Month,
            dateTime.Day,
            dateTime.Hour,
            dateTime.Minute,
            0);

        var remainder = trimmed.Minute % 15;

        if (remainder == 0)
            return trimmed > dateTime ? trimmed : trimmed;

        return trimmed.AddMinutes(15 - remainder);
    }
    public async Task<bool> IsTimeSlotAvailableAsync(DateTime startTime, int durationMinutes, int? excludeAppointmentId = null)
    {
        var endTime = startTime.AddMinutes(durationMinutes);

        var conflict = await _context.Appointments.AnyAsync(a =>
            (!excludeAppointmentId.HasValue || a.Id != excludeAppointmentId.Value) &&
            startTime < a.StartTime.AddMinutes(a.DurationMinutes) &&
            endTime > a.StartTime);

        return !conflict;
    }

}