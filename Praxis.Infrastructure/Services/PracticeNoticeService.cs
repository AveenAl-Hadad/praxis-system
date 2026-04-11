using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;
public class PracticeNoticeService : IPracticeNoticeService
{
    private readonly PraxisDbContext _db;

    public PracticeNoticeService(PraxisDbContext db)
    {
        _db = db;
    }

    public async Task<List<PracticeNotice>> GetActiveNoticesAsync()
    {
        var now = DateTime.Now;

        return await _db.PracticeNotices
            .Where(n => n.IsActive)
            .Where(n => n.VisibleUntil == null || n.VisibleUntil >= now)
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task AddNoticeAsync(PracticeNotice notice)
    {
        _db.PracticeNotices.Add(notice);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateNoticeAsync(PracticeNotice notice)
    {
        var existing = await _db.PracticeNotices.FindAsync(notice.Id);
        if (existing == null)
            throw new InvalidOperationException("Hinweis wurde nicht gefunden.");

        existing.Title = notice.Title;
        existing.Content = notice.Content;
        existing.Category = notice.Category;
        existing.IsPinned = notice.IsPinned;
        existing.IsActive = notice.IsActive;
        existing.VisibleUntil = notice.VisibleUntil;

        await _db.SaveChangesAsync();
    }

    public async Task DeactivateNoticeAsync(int noticeId)
    {
        var existing = await _db.PracticeNotices.FindAsync(noticeId);
        if (existing == null)
            throw new InvalidOperationException("Hinweis wurde nicht gefunden.");

        existing.IsActive = false;
        await _db.SaveChangesAsync();
    }
    public async Task DeleteNoticeAsync(int noticeId)
    {
        var existing = await _db.PracticeNotices.FindAsync(noticeId);
        if (existing == null)
            throw new InvalidOperationException("Hinweis wurde nicht gefunden.");

        _db.PracticeNotices.Remove(existing);
        await _db.SaveChangesAsync();
    }
}