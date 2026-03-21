using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;
using Xunit;

namespace Praxis.Tests;

public class AuditServiceTests
{
    // Erstellt eine InMemory-Datenbank für Tests.
    // Jeder Test bekommt seine eigene Datenbank.
    private static PraxisDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<PraxisDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new PraxisDbContext(options);
    }

    [Fact]
    public async Task LogAsync_ShouldSaveAuditLog()
    {
        // Arrange:
        // Test-Datenbank und Service erstellen
        using var context = CreateInMemoryContext();
        var service = new AuditService(context);

        // Act:
        // Einen neuen Audit-Log-Eintrag speichern
        await service.LogAsync(
            "tester",
            "CREATE",
            "Patient",
            "Patient Max Mustermann wurde erstellt.");

        // Assert:
        // Prüfen, ob genau ein Eintrag gespeichert wurde
        var logs = await context.AuditLogs.ToListAsync();
        var log = Assert.Single(logs);

        Assert.Equal("tester", log.UserName);
        Assert.Equal("CREATE", log.Action);
        Assert.Equal("Patient", log.EntityType);
        Assert.Equal("Patient Max Mustermann wurde erstellt.", log.Details);

        // Prüfen, ob ein Zeitstempel gesetzt wurde
        Assert.True(log.Timestamp <= DateTime.Now);
    }

    [Fact]
    public async Task GetLogsAsync_ShouldReturnLogsOrderedByTimestampDescending()
    {
        // Arrange:
        // Test-Datenbank und Service erstellen
        using var context = CreateInMemoryContext();
        var service = new AuditService(context);

        // Zwei Logs direkt in die Datenbank schreiben
        // Wichtig: verschiedene Zeitpunkte, damit wir die Reihenfolge testen können
        context.AuditLogs.AddRange(
            new AuditLog
            {
                Timestamp = new DateTime(2024, 1, 1, 10, 0, 0),
                UserName = "user1",
                Action = "CREATE",
                EntityType = "Patient",
                Details = "Erster Eintrag"
            },
            new AuditLog
            {
                Timestamp = new DateTime(2024, 1, 1, 11, 0, 0),
                UserName = "user2",
                Action = "DELETE",
                EntityType = "Appointment",
                Details = "Zweiter Eintrag"
            });
        await context.SaveChangesAsync();

        // Act:
        // Logs über den Service laden
        var result = await service.GetLogsAsync();

        // Assert:
        // Es sollen 2 Logs zurückkommen
        Assert.Equal(2, result.Count);

        // Neuester Eintrag muss zuerst kommen
        Assert.Equal("Zweiter Eintrag", result[0].Details);
        Assert.Equal("Erster Eintrag", result[1].Details);
    }

    [Fact]
    public async Task GetLogsAsync_ShouldReturnEmptyList_WhenNoLogsExist()
    {
        // Arrange:
        // Leere Test-Datenbank
        using var context = CreateInMemoryContext();
        var service = new AuditService(context);

        // Act:
        // Logs laden, obwohl keine vorhanden sind
        var result = await service.GetLogsAsync();

        // Assert:
        // Ergebnis soll leer sein
        Assert.Empty(result);
    }

    [Fact]
    public async Task LogAsync_ShouldSaveMultipleAuditLogs()
    {
        // Arrange:
        using var context = CreateInMemoryContext();
        var service = new AuditService(context);

        // Act:
        // Mehrere Logs speichern
        await service.LogAsync("tester1", "CREATE", "Patient", "Patient erstellt");
        await service.LogAsync("tester2", "UPDATE", "Patient", "Patient geändert");
        await service.LogAsync("tester3", "DELETE", "Appointment", "Termin gelöscht");

        // Assert:
        // Prüfen, ob alle 3 Einträge gespeichert wurden
        var logs = await context.AuditLogs.ToListAsync();

        Assert.Equal(3, logs.Count);
        Assert.Contains(logs, x => x.Action == "CREATE" && x.UserName == "tester1");
        Assert.Contains(logs, x => x.Action == "UPDATE" && x.UserName == "tester2");
        Assert.Contains(logs, x => x.Action == "DELETE" && x.UserName == "tester3");
    }
}