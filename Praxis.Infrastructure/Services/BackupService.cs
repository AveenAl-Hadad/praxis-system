using Microsoft.EntityFrameworkCore;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly PraxisDbContext _dbContext;

    public BackupService(PraxisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private string GetDatabasePath()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "praxis.db");
    }

    public async Task CreateBackupAsync(string backupFilePath)
    {
        var dbPath = GetDatabasePath();

        await _dbContext.Database.CloseConnectionAsync();

        File.Copy(dbPath, backupFilePath, true);
    }

    public async Task RestoreBackupAsync(string backupFilePath)
    {
        var dbPath = GetDatabasePath();

        if (!File.Exists(backupFilePath))
            throw new FileNotFoundException("Backup-Datei wurde nicht gefunden.");

        // WICHTIG: DB komplett schließen
        await _dbContext.Database.CloseConnectionAsync();
        _dbContext.Dispose();

        // kleine Pause (wichtig bei Windows File Lock)
        await Task.Delay(500);

        // Datei überschreiben
        File.Copy(backupFilePath, dbPath, true);
    }
}