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
        _context.Appointments.Add(appointment);
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

    public async Task<List<Appointment>> GetAllAppointmentsAsync()
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .OrderBy(a => a.StartTime)
            .ToListAsync();
    }
}