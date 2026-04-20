using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;
using Xunit;

namespace Praxis.Tests;

public class PrescriptionServiceTests
{
    // Erstellt eine InMemory-Datenbank für Tests
    private static PraxisDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<PraxisDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new PraxisDbContext(options);
    }

    // Hilfsmethode: Beispiel-Patient erstellen
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

    // Hilfsmethode: Beispiel-Rezept erstellen
    private static Prescription CreatePrescription(
        int patientId,
        DateTime? issueDate = null,
        string prescriptionNumber = "RX-1001",
        string medicationName = "Ibuprofen",
        string dosage = "400mg",
        string instructions = "2x täglich",
        string doctorName = "Dr. Müller")
    {
        return new Prescription
        {
            PatientId = patientId,
            IssueDate = issueDate ?? DateTime.Now,
            PrescriptionNumber = prescriptionNumber,
            MedicationName = medicationName,
            Dosage = dosage,
            Instructions = instructions,
            DoctorName = doctorName
        };
    }

    [Fact]
    public async Task GetAllPrescriptionsAsync_ShouldReturnAllPrescriptions_OrderedByIssueDateDescending()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        context.Prescriptions.AddRange(
            CreatePrescription(patient.Id, new DateTime(2024, 1, 1), "RX-OLD"),
            CreatePrescription(patient.Id, new DateTime(2024, 1, 3), "RX-NEW"),
            CreatePrescription(patient.Id, new DateTime(2024, 1, 2), "RX-MID"));
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new PrescriptionService(context, auditMock.Object);

        // Act:
        var result = await service.GetAllPrescriptionsAsync();

        // Assert:
        Assert.Equal(3, result.Count);
        Assert.Equal("RX-NEW", result[0].PrescriptionNumber);
        Assert.Equal("RX-MID", result[1].PrescriptionNumber);
        Assert.Equal("RX-OLD", result[2].PrescriptionNumber);
    }

    [Fact]
    public async Task GetPrescriptionsByPatientAsync_ShouldReturnOnlyPrescriptionsOfSelectedPatient()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var patient1 = CreatePatient("Max", "Mustermann", "max@test.de", "111");
        var patient2 = CreatePatient("Anna", "Meyer", "anna@test.de", "222");
        context.Patients.AddRange(patient1, patient2);
        await context.SaveChangesAsync();

        context.Prescriptions.AddRange(
            CreatePrescription(patient1.Id, new DateTime(2024, 1, 2), "RX-P1-1"),
            CreatePrescription(patient1.Id, new DateTime(2024, 1, 3), "RX-P1-2"),
            CreatePrescription(patient2.Id, new DateTime(2024, 1, 4), "RX-P2-1"));
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new PrescriptionService(context, auditMock.Object);

        // Act:
        var result = await service.GetPrescriptionsByPatientAsync(patient1.Id);

        // Assert:
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal(patient1.Id, p.PatientId));

        // Da absteigend nach Datum sortiert wird, muss das neuere zuerst kommen
        Assert.Equal("RX-P1-2", result[0].PrescriptionNumber);
        Assert.Equal("RX-P1-1", result[1].PrescriptionNumber);
    }

    [Fact]
    public async Task GetPrescriptionByIdAsync_ShouldReturnMatchingPrescription()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var prescription = CreatePrescription(patient.Id, prescriptionNumber: "RX-DETAIL");
        context.Prescriptions.Add(prescription);
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new PrescriptionService(context, auditMock.Object);

        // Act:
        var result = await service.GetPrescriptionByIdAsync(prescription.Id);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(prescription.Id, result!.Id);
        Assert.Equal("RX-DETAIL", result.PrescriptionNumber);
        Assert.Equal(patient.Id, result.PatientId);
    }

    [Fact]
    public async Task AddPrescriptionAsync_ShouldSavePrescription_AndWriteAuditLog()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new PrescriptionService(context, auditMock.Object);

        var prescription = CreatePrescription(
            patient.Id,
            prescriptionNumber: "RX-2001",
            medicationName: "Paracetamol");

        // Act:
        await service.AddPrescriptionAsync(prescription, "tester");

        // Assert:
        var savedPrescription = await context.Prescriptions.FirstAsync();

        Assert.Equal(patient.Id, savedPrescription.PatientId);
        Assert.Equal("RX-2001", savedPrescription.PrescriptionNumber);
        Assert.Equal("Paracetamol", savedPrescription.MedicationName);

        auditMock.Verify(a => a.LogAsync(
                "tester",
                "CREATE",
                "Prescription",
                It.Is<string>(m => m.Contains($"Patient {patient.Id}"))),
            Times.Once);
    }

    [Fact]
    public async Task AddPrescriptionAsync_ShouldGeneratePrescriptionNumber_WhenEmpty()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new PrescriptionService(context, auditMock.Object);

        var prescription = CreatePrescription(
            patient.Id,
            prescriptionNumber: "");

        // Act:
        await service.AddPrescriptionAsync(prescription, "tester");

        // Assert:
        var savedPrescription = await context.Prescriptions.FirstAsync();

        Assert.False(string.IsNullOrWhiteSpace(savedPrescription.PrescriptionNumber));
        Assert.StartsWith("RX-", savedPrescription.PrescriptionNumber);
    }

    [Fact]
    public async Task DeletePrescriptionAsync_ShouldRemovePrescription_AndWriteAuditLog()
    {
        // Arrange:
        // Test-Datenbank erstellen
        using var context = CreateInMemoryContext();

        // Patient anlegen
        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        // Rezept anlegen
        var prescription = CreatePrescription(patient.Id, prescriptionNumber: "RX-DELETE");
        context.Prescriptions.Add(prescription);
        await context.SaveChangesAsync();

        // AuditService mocken
        var auditMock = new Mock<IAuditService>();
        var service = new PrescriptionService(context, auditMock.Object);

        // Act:
        // Rezept löschen
        await service.DeletePrescriptionAsync(prescription.Id, "tester");

        // Assert:
        // Rezept soll aus der Datenbank entfernt worden sein
        Assert.Empty(context.Prescriptions);

        // Prüfen, ob das Audit-Log geschrieben wurde
        auditMock.Verify(a => a.LogAsync(
                "tester",
                "DELETE",
                "Prescription",
                It.Is<string>(m => m.Contains($"Rezept {prescription.PrescriptionNumber} gelöscht"))),
            Times.Once);
    }

    [Fact]
    public async Task DeletePrescriptionAsync_ShouldDoNothing_WhenPrescriptionDoesNotExist()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var auditMock = new Mock<IAuditService>();
        var service = new PrescriptionService(context, auditMock.Object);

        // Act:
        await service.DeletePrescriptionAsync(999, "tester");

        // Assert:
        Assert.Empty(context.Prescriptions);

        // Es darf kein Audit-Log geschrieben werden, weil nichts gelöscht wurde
        auditMock.Verify(a => a.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }
}