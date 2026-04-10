using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;
/// <summary>
/// Service für Audit-Logging.
/// Speichert Benutzeraktionen (z.B. Create, Update, Delete) in der Datenbank.
/// </summary>
public class AuditService : IAuditService
{
    private readonly PraxisDbContext _db;

    /// <summary>
    /// Konstruktor mit Dependency Injection für den DbContext.
    /// </summary>
    public AuditService(PraxisDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Speichert einen neuen Audit-Log-Eintrag.
    /// </summary>
    /// <param name="userName">Benutzer, der die Aktion ausgeführt hat</param>
    /// <param name="action">Aktion (z.B. CREATE, UPDATE, DELETE)</param>
    /// <param name="entityType">Betroffene Entität (z.B. Patient)</param>
    /// <param name="details">Beschreibung der Aktion</param>
    public async Task LogAsync(string userName, string action, string entityType, string details)
    {
        var log = new AuditLog
        {
            Timestamp = DateTime.Now, // Zeitpunkt der Aktion
            UserName = userName,
            Action = action,
            EntityType = entityType,
            Details = details
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Gibt alle Audit-Logs zurück (neueste zuerst).
    /// </summary>
    public async Task<List<AuditLog>> GetLogsAsync()
    {
        return await _db.AuditLogs
            .OrderByDescending(x => x.Timestamp) // neueste Einträge zuerst
            .ToListAsync();
    }
}