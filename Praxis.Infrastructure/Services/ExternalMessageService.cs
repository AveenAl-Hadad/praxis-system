using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class ExternalMessageService : IExternalMessageService
{
    private readonly PraxisDbContext _db;

    public ExternalMessageService(PraxisDbContext db)
    {
        _db = db;
    }

    public async Task<List<ExternalMessage>> GetAllAsync()
    {
        return await _db.ExternalMessages
            .Include(x => x.Patient)
            .OrderByDescending(x => x.ReceivedAt)
            .ToListAsync();
    }

    public async Task AddAsync(ExternalMessage message)
    {
        message.ReceivedAt = DateTime.Now;
        _db.ExternalMessages.Add(message);
        await _db.SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(int id)
    {
        var message = await _db.ExternalMessages.FindAsync(id);

        if (message == null)
            return;

        message.IsRead = true;
        await _db.SaveChangesAsync();
    }

    public async Task MarkAsProcessedAsync(int id)
    {
        var message = await _db.ExternalMessages.FindAsync(id);

        if (message == null)
            return;

        message.Status = "Bearbeitet";
        message.IsRead = true;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var message = await _db.ExternalMessages.FindAsync(id);

        if (message == null)
            return;

        _db.ExternalMessages.Remove(message);
        await _db.SaveChangesAsync();
    }
    public async Task AssignPatientAsync(int messageId, int patientId)
    {
        var message = await _db.ExternalMessages.FindAsync(messageId);

        if (message == null)
            return;

        message.PatientId = patientId;
        await _db.SaveChangesAsync();
    }
}