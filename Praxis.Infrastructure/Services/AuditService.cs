using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly PraxisDbContext _db;

    public AuditService(PraxisDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(string userName, string action, string entityType, string details)
    {
        var log = new AuditLog
        {
            Timestamp = DateTime.Now,
            UserName = userName,
            Action = action,
            EntityType = entityType,
            Details = details
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetLogsAsync()
    {
        return await _db.AuditLogs
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync();
    }

   
}