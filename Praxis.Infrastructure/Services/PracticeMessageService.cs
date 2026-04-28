using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class PracticeMessageService : IPracticeMessageService
{
    private readonly PraxisDbContext _db;

    public PracticeMessageService(PraxisDbContext db)
    {
        _db = db;
    }

    public async Task<List<PracticeMessage>> GetInboxAsync(string recipient)
    {
        return await _db.PracticeMessages
            .Include(x => x.Patient)
            .Where(x => x.Recipient == recipient)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<PracticeMessage>> GetSentAsync(string sender)
    {
        return await _db.PracticeMessages
            .Include(x => x.Patient)
            .Where(x => x.Sender == sender)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task SendAsync(PracticeMessage message)
    {
        message.CreatedAt = DateTime.Now;
        _db.PracticeMessages.Add(message);
        await _db.SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(int id)
    {
        var message = await _db.PracticeMessages.FindAsync(id);

        if (message == null)
            return;

        message.IsRead = true;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var message = await _db.PracticeMessages.FindAsync(id);

        if (message == null)
            return;

        _db.PracticeMessages.Remove(message);
        await _db.SaveChangesAsync();
    }
}