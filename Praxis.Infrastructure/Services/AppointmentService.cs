using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

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
    
    public async Task<Appointment?> GetAppointmentByIdAsync(int id)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == id);
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
        var newStart = appointment.StartTime;
        var newEnd = appointment.StartTime.AddMinutes(appointment.DurationMinutes);

        var conflict = await _context.Appointments.AnyAsync(a =>
            a.Id != appointment.Id &&
            newStart < a.StartTime.AddMinutes(a.DurationMinutes) &&
            newEnd > a.StartTime);

        if (conflict)
            throw new InvalidOperationException("Es existiert bereits ein Termin in diesem Zeitraum.");
    }

}