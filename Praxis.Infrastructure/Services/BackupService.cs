using Microsoft.EntityFrameworkCore;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

/// <summary>
/// Service für Backup und Wiederherstellung der SQLite-Datenbank.
/// </summary>
public class BackupService : IBackupService
{
    private readonly PraxisDbContext _dbContext;

    /// <summary>
    /// Konstruktor mit Dependency Injection für den DbContext.
    /// </summary>
    public BackupService(PraxisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Ermittelt den Pfad zur SQLite-Datenbankdatei.
    /// </summary>
    private string GetDatabasePath()
    {
        return Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "praxis.db");
    }

    /// <summary>
    /// Erstellt ein Backup der Datenbank (Dateikopie).
    /// </summary>
    /// <param name="backupFilePath">Zielpfad für das Backup</param>
    public async Task CreateBackupAsync(string backupFilePath)
    {
        var dbPath = GetDatabasePath();

        // Verbindung schließen, damit keine Locks bestehen
        await _dbContext.Database.CloseConnectionAsync();

        // Datenbankdatei kopieren (überschreibt bestehende Datei)
        File.Copy(dbPath, backupFilePath, true);
    }

    /// <summary>
    /// Stellt ein Backup wieder her (überschreibt die aktuelle DB).
    /// </summary>
    /// <param name="backupFilePath">Pfad zur Backup-Datei</param>
    public async Task RestoreBackupAsync(string backupFilePath)
    {
        var dbPath = GetDatabasePath();

        // Prüfen, ob Backup existiert
        if (!File.Exists(backupFilePath))
            throw new FileNotFoundException("Backup-Datei wurde nicht gefunden.");

        // WICHTIG: Verbindung vollständig schließen
        await _dbContext.Database.CloseConnectionAsync();

        // DbContext freigeben (verhindert File Locks)
        _dbContext.Dispose();

        // Kleine Pause → wichtig bei Windows wegen File-Locking
        await Task.Delay(500);

        // Backup-Datei über die aktuelle DB kopieren
        File.Copy(backupFilePath, dbPath, true);
    }
}