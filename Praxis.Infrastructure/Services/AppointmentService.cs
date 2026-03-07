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
}