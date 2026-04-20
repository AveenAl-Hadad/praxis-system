using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Exceptions;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;
using Xunit;

namespace Praxis.Tests;

public class PatientServiceTests
{
    // Erstellt eine InMemory-Datenbank für Tests.
    // Jeder Test bekommt dadurch seine eigene kleine Test-Datenbank.
    private static PraxisDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<PraxisDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new PraxisDbContext(options);
    }

    // Hilfsmethode: Erstellt einen Beispiel-Patienten.
    // So müssen wir in den Tests nicht immer alle Felder neu schreiben.
    private static Patient CreatePatient(
        string vorname = "Max",
        string nachname = "Mustermann",
        string email = "max@test.de",
        string telefon = "123456789")
    {
        return new Patient
        {
            Vorname = vorname,
            Nachname = nachname,
            Geburtsdatum = new DateTime(1990, 1, 1),
            Email = email,
            Telefonnummer = telefon,
            IsActive = true
        };
    }

    [Fact]
    public async Task GetAllPatientsAsync_ShouldReturnAllPatients()
    {
        // Arrange:
        // Test-Datenbank erstellen und 2 Patienten speichern
        using var context = CreateInMemoryContext();
        context.Patients.AddRange(
            CreatePatient("Anna", "Schmidt", "anna@test.de", "111"),
            CreatePatient("Ben", "Meyer", "ben@test.de", "222"));
        await context.SaveChangesAsync();

        // AuditService wird hier nur "gefälscht", damit wir den PatientService bauen können
        var auditMock = new Mock<IAuditService>();
        var service = new PatientService(context, auditMock.Object);

        // Act:
        // Methode aufrufen, die alle Patienten laden soll
        var result = (await service.GetAllPatientsAsync()).ToList();

        // Assert:
        // Prüfen, ob wirklich 2 Patienten zurückkommen
        Assert.Equal(2, result.Count);

        // Prüfen, ob Anna und Ben in den Ergebnissen vorhanden sind
        Assert.Contains(result, p => p.Vorname == "Anna");
        Assert.Contains(result, p => p.Vorname == "Ben");
    }

    [Fact]
    public async Task AddPatientAsync_ShouldSavePatient_AndWriteAuditLog()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        // Mock von AuditService:
        // Damit können wir später prüfen, ob LogAsync aufgerufen wurde
        var auditMock = new Mock<IAuditService>();
        var service = new PatientService(context, auditMock.Object);

        var patient = CreatePatient();

        // Act:
        // Patient speichern
        await service.AddPatientAsync(patient, "tester");

        // Assert:
        // Prüfen, ob der Patient in der Datenbank gespeichert wurde
        var savedPatient = await context.Patients.FirstOrDefaultAsync();

        Assert.NotNull(savedPatient);
        Assert.Equal("Max", savedPatient!.Vorname);
        Assert.Equal("Mustermann", savedPatient.Nachname);
        Assert.Equal("max@test.de", savedPatient.Email);

        // Prüfen, ob auch ein Audit-Log geschrieben wurde
        auditMock.Verify(a => a.LogAsync(
                "tester",
                "CREATE",
                "Patient",
                It.Is<string>(m => m.Contains("Max") && m.Contains("Mustermann"))),
            Times.Once);
    }

    [Fact]
    public async Task AddPatientAsync_ShouldThrowUserFriendlyException_WhenEmailAlreadyExists()
    {
        // Arrange:
        // Schon einen Patienten mit dieser E-Mail speichern
        using var context = CreateInMemoryContext();
        context.Patients.Add(CreatePatient("Anna", "Meyer", "doppelt@test.de", "111"));
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new PatientService(context, auditMock.Object);

        // Neuer Patient mit derselben E-Mail
        var duplicate = CreatePatient("Ben", "Schulz", "doppelt@test.de", "222");

        // Act + Assert:
        // Es soll eine benutzerfreundliche Exception geworfen werden
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            service.AddPatientAsync(duplicate, "tester"));

        Assert.Equal("Diese E-Mail ist bereits vorhanden.", ex.Message);
    }

    [Fact]
    public async Task SearchPatientsAsync_ShouldFindPatients_CaseInsensitive()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        // Zwei Patienten speichern
        context.Patients.AddRange(
            CreatePatient("Anna", "Schmidt", "anna@test.de", "111"),
            CreatePatient("Peter", "Meyer", "peter@test.de", "222"));
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new PatientService(context, auditMock.Object);

        // Act:
        // Suche mit Großbuchstaben, obwohl Patient "Anna" normal geschrieben ist
        var result = await service.SearchPatientsAsync("ANNA");

        // Assert:
        // Die Suche soll trotzdem funktionieren
        Assert.Single(result);
        Assert.Equal("Anna", result[0].Vorname);
    }

    [Fact]
    public async Task UpdatePatientAsync_ShouldUpdateExistingPatient()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        // Alten Patienten speichern
        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new PatientService(context, auditMock.Object);

        // Neues Objekt mit geänderten Daten
        var updatedPatient = new Patient
        {
            Id = patient.Id, // gleiche Id -> Patient wird aktualisiert
            Vorname = "Erika",
            Nachname = "Mustermann",
            Geburtsdatum = new DateTime(1988, 5, 10),
            Email = "erika@test.de",
            Telefonnummer = "999",
            IsActive = false
        };

        // Act:
        // Update-Methode aufrufen
        await service.UpdatePatientAsync(updatedPatient);

        // Assert:
        // Prüfen, ob die Daten wirklich geändert wurden
        var savedPatient = await context.Patients.FirstAsync();
        Assert.Equal("Erika", savedPatient.Vorname);
        Assert.Equal("erika@test.de", savedPatient.Email);
        Assert.Equal("999", savedPatient.Telefonnummer);
        Assert.False(savedPatient.IsActive);
    }

    [Fact]
    public async Task ToggleActiveAsync_ShouldInvertIsActive()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        // Patient ist anfangs aktiv
        var patient = CreatePatient();
        patient.IsActive = true;
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new PatientService(context, auditMock.Object);

        // Act:
        // Aktiv-Status umschalten
        await service.ToggleActiveAsync(patient.Id);

        // Assert:
        // Aus true soll false werden
        var updatedPatient = await context.Patients.FirstAsync();
        Assert.False(updatedPatient.IsActive);
    }

    [Fact]
    public async Task DeletePatientAsync_ShouldRemovePatient_AndWriteAuditLog()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        // Einen Patienten speichern
        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new PatientService(context, auditMock.Object);

        // Act:
        // Patient löschen
        await service.DeletePatientAsync(patient.Id, "tester");

        // Assert:
        // Datenbank soll leer sein
        Assert.Empty(context.Patients);

        // Zusätzlich soll ein Audit-Log geschrieben worden sein
        auditMock.Verify(a => a.LogAsync(
                "tester",
                "DELETE",
                "Patient",
                It.Is<string>(m => m.Contains("Max") && m.Contains("Mustermann"))),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePatientAsync_ShouldThrowUserFriendlyException_WhenUniqueConstraintIsViolated()
    {
        // Dieser Test benutzt NICHT die InMemory-DB,
        // sondern SQLite in Memory.
        // Warum?
        // Weil nur SQLite echte Unique-Constraints wie eine richtige DB prüft.

        // Arrange:
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PraxisDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new PraxisDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Zwei verschiedene Patienten speichern
        var p1 = CreatePatient("Anna", "Schmidt", "anna@test.de", "111");
        var p2 = CreatePatient("Ben", "Meyer", "ben@test.de", "222");
        context.Patients.AddRange(p1, p2);
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new PatientService(context, auditMock.Object);

        // Jetzt versuchen wir, Patient 2 so zu ändern,
        // dass er dieselbe E-Mail wie Patient 1 bekommt
        var updatedPatient = new Patient
        {
            Id = p2.Id,
            Vorname = p2.Vorname,
            Nachname = p2.Nachname,
            Geburtsdatum = p2.Geburtsdatum,
            Email = "anna@test.de", // doppelte E-Mail
            Telefonnummer = p2.Telefonnummer,
            IsActive = p2.IsActive
        };

        // Act + Assert:
        // Es soll eine UserFriendlyException kommen
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            service.UpdatePatientAsync(updatedPatient));

        Assert.Equal("Update fehlgeschlagen: E-Mail oder Telefonnummer bereits vorhanden.", ex.Message);
    }
}