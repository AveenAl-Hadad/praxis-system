using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;
using Xunit;

namespace Praxis.Tests;

public class BackupServiceTests
{
    private static PraxisDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PraxisDbContext>()
            .UseSqlite("Data Source=praxis.db") // echte Datei!
            .Options;

        return new PraxisDbContext(options);
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldCreateBackupFile()
    {
        // Arrange:
        // Erstelle echten DbContext (weil File.Copy echte Datei braucht)
        using var context = CreateContext();

        // Datenbank erstellen, falls sie noch nicht existiert
        await context.Database.EnsureCreatedAsync();

        var service = new BackupService(context);

        // Zielpfad für Backup (temporäre Datei)
        var backupPath = Path.Combine(Path.GetTempPath(), "backup_test.db");

        // Falls Datei existiert, löschen
        if (File.Exists(backupPath))
            File.Delete(backupPath);

        // Act:
        // Backup erstellen
        await service.CreateBackupAsync(backupPath);

        // Assert:
        // Prüfen, ob Backup-Datei existiert
        Assert.True(File.Exists(backupPath));
    }

    [Fact]
    public async Task RestoreBackupAsync_ShouldThrow_WhenFileDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var service = new BackupService(context);

        var fakePath = Path.Combine(Path.GetTempPath(), "does_not_exist.db");

        // Act + Assert:
        // Wenn Datei nicht existiert → Exception erwartet
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.RestoreBackupAsync(fakePath));

        Assert.Equal("Backup-Datei wurde nicht gefunden.", ex.Message);
    }
}