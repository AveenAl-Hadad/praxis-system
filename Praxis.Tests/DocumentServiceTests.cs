using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;
using Xunit;

namespace Praxis.Tests;

public class DocumentServiceTests
{
    // Erstellt eine InMemory-Datenbank für Tests
    private static PraxisDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<PraxisDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new PraxisDbContext(options);
    }

    // Hilfsmethode: Beispiel-Patient
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

    // Hilfsmethode: Beispiel-Dokument
    private static PatientDocument CreateDocument(
        int patientId,
        string fileName = "test.pdf",
        string filePath = @"C:\Docs\test.pdf",
        DateTime? uploadDate = null)
    {
        return new PatientDocument
        {
            PatientId = patientId,
            FileName = fileName,
            FilePath = filePath,
            UploadDate = uploadDate ?? DateTime.Now
        };
    }

    [Fact]
    public async Task GetDocumentsByPatientAsync_ShouldReturnOnlyDocumentsOfSelectedPatient_OrderedByNewestFirst()
    {
        // Arrange:
        // Test-Datenbank und Service erstellen
        using var context = CreateInMemoryContext();
        var service = new DocumentService(context);

        // Zwei Patienten anlegen
        var patient1 = CreatePatient("Max", "Mustermann", "max@test.de", "111");
        var patient2 = CreatePatient("Anna", "Meyer", "anna@test.de", "222");

        context.Patients.AddRange(patient1, patient2);
        await context.SaveChangesAsync();

        // Dokumente anlegen:
        // Zwei für patient1, eines für patient2
        context.PatientDocuments.AddRange(
            CreateDocument(patient1.Id, "alt.pdf", @"C:\Docs\alt.pdf", new DateTime(2024, 1, 1, 10, 0, 0)),
            CreateDocument(patient1.Id, "neu.pdf", @"C:\Docs\neu.pdf", new DateTime(2024, 1, 2, 10, 0, 0)),
            CreateDocument(patient2.Id, "andererPatient.pdf", @"C:\Docs\anderer.pdf", new DateTime(2024, 1, 3, 10, 0, 0))
        );
        await context.SaveChangesAsync();

        // Act:
        // Nur Dokumente von patient1 laden
        var result = await service.GetDocumentsByPatientAsync(patient1.Id);

        // Assert:
        // Es sollen genau 2 Dokumente zurückkommen
        Assert.Equal(2, result.Count);

        // Neueste zuerst
        Assert.Equal("neu.pdf", result[0].FileName);
        Assert.Equal("alt.pdf", result[1].FileName);

        // Prüfen, dass wirklich nur Dokumente von patient1 enthalten sind
        Assert.All(result, d => Assert.Equal(patient1.Id, d.PatientId));
    }

    [Fact]
    public async Task AddDocumentAsync_ShouldSaveDocument()
    {
        // Arrange:
        using var context = CreateInMemoryContext();
        var service = new DocumentService(context);

        // Patient anlegen
        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        // Dokument erstellen
        var document = CreateDocument(
            patient.Id,
            "bericht.pdf",
            @"C:\Docs\bericht.pdf");

        // Act:
        // Dokument speichern
        await service.AddDocumentAsync(document);

        // Assert:
        // Prüfen, ob das Dokument wirklich gespeichert wurde
        var savedDocument = await context.PatientDocuments.FirstAsync();

        Assert.Equal(patient.Id, savedDocument.PatientId);
        Assert.Equal("bericht.pdf", savedDocument.FileName);
        Assert.Equal(@"C:\Docs\bericht.pdf", savedDocument.FilePath);
    }

    [Fact]
    public async Task DeleteDocumentAsync_ShouldRemoveDocument_WhenDocumentExists()
    {
        // Arrange:
        using var context = CreateInMemoryContext();
        var service = new DocumentService(context);

        // Patient anlegen
        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        // Dokument speichern
        var document = CreateDocument(patient.Id, "delete.pdf", @"C:\Docs\delete.pdf");
        context.PatientDocuments.Add(document);
        await context.SaveChangesAsync();

        // Act:
        // Dokument löschen
        await service.DeleteDocumentAsync(document.Id);

        // Assert:
        // Datenbank soll danach leer sein
        Assert.Empty(context.PatientDocuments);
    }

    [Fact]
    public async Task DeleteDocumentAsync_ShouldDoNothing_WhenDocumentDoesNotExist()
    {
        // Arrange:
        using var context = CreateInMemoryContext();
        var service = new DocumentService(context);

        // Act:
        // Eine nicht vorhandene ID löschen
        await service.DeleteDocumentAsync(999);

        // Assert:
        // Es soll kein Fehler kommen und die Tabelle bleibt leer
        Assert.Empty(context.PatientDocuments);
    }

    [Fact]
    public async Task GetDocumentsByPatientAsync_ShouldReturnEmptyList_WhenPatientHasNoDocuments()
    {
        // Arrange:
        using var context = CreateInMemoryContext();
        var service = new DocumentService(context);

        // Patient anlegen, aber kein Dokument speichern
        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        // Act:
        var result = await service.GetDocumentsByPatientAsync(patient.Id);

        // Assert:
        // Ergebnis soll leer sein
        Assert.Empty(result);
    }
}