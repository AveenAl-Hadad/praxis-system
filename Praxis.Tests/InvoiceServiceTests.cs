using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;
using Xunit;

namespace Praxis.Tests;

public class InvoiceServiceTests
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

    // Hilfsmethode: Beispiel-Rechnungsposition
    private static InvoiceItem CreateInvoiceItem(
        string description = "Behandlung",
        int quantity = 1,
        decimal unitPrice = 100m)
    {
        return new InvoiceItem
        {
            Description = description,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    // Hilfsmethode: Beispiel-Rechnung
    private static Invoice CreateInvoice(
        int patientId,
        DateTime? invoiceDate = null,
        string invoiceNumber = "RE-1001",
        List<InvoiceItem>? items = null)
    {
        return new Invoice
        {
            PatientId = patientId,
            InvoiceDate = invoiceDate ?? DateTime.Now,
            InvoiceNumber = invoiceNumber,
            Items = items ?? new List<InvoiceItem>
            {
                CreateInvoiceItem("Behandlung", 1, 100m)
            }
        };
    }

    [Fact]
    public async Task GetAllInvoicesAsync_ShouldReturnAllInvoices_OrderedByInvoiceDateDescending()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        context.Invoices.AddRange(
            CreateInvoice(patient.Id, new DateTime(2024, 1, 1), "RE-OLD"),
            CreateInvoice(patient.Id, new DateTime(2024, 1, 3), "RE-NEW"),
            CreateInvoice(patient.Id, new DateTime(2024, 1, 2), "RE-MID"));
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new InvoiceService(context, auditMock.Object);

        // Act:
        var result = await service.GetAllInvoicesAsync();

        // Assert:
        Assert.Equal(3, result.Count);
        Assert.Equal("RE-NEW", result[0].InvoiceNumber);
        Assert.Equal("RE-MID", result[1].InvoiceNumber);
        Assert.Equal("RE-OLD", result[2].InvoiceNumber);
    }

    [Fact]
    public async Task GetInvoicesByPatientAsync_ShouldReturnOnlyInvoicesOfSelectedPatient()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var patient1 = CreatePatient("Max", "Mustermann", "max@test.de", "111");
        var patient2 = CreatePatient("Anna", "Meyer", "anna@test.de", "222");

        context.Patients.AddRange(patient1, patient2);
        await context.SaveChangesAsync();

        context.Invoices.AddRange(
            CreateInvoice(patient1.Id, new DateTime(2024, 1, 2), "RE-P1-1"),
            CreateInvoice(patient1.Id, new DateTime(2024, 1, 3), "RE-P1-2"),
            CreateInvoice(patient2.Id, new DateTime(2024, 1, 4), "RE-P2-1"));
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new InvoiceService(context, auditMock.Object);

        // Act:
        var result = await service.GetInvoicesByPatientAsync(patient1.Id);

        // Assert:
        Assert.Equal(2, result.Count);
        Assert.All(result, i => Assert.Equal(patient1.Id, i.PatientId));
        Assert.Equal("RE-P1-2", result[0].InvoiceNumber);
        Assert.Equal("RE-P1-1", result[1].InvoiceNumber);
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_ShouldReturnMatchingInvoice()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var invoice = CreateInvoice(patient.Id, invoiceNumber: "RE-DETAIL");
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new InvoiceService(context, auditMock.Object);

        // Act:
        var result = await service.GetInvoiceByIdAsync(invoice.Id);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(invoice.Id, result!.Id);
        Assert.Equal("RE-DETAIL", result.InvoiceNumber);
        Assert.Equal(patient.Id, result.PatientId);
        Assert.NotNull(result.Items);
    }

    [Fact]
    public async Task AddInvoiceAsync_ShouldSaveInvoice_CalculateTotalAmount_AndWriteAuditLog()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new InvoiceService(context, auditMock.Object);

        var invoice = CreateInvoice(
            patient.Id,
            invoiceNumber: "RE-2001",
            items: new List<InvoiceItem>
            {
                CreateInvoiceItem("Leistung 1", 1, 100m),
                CreateInvoiceItem("Leistung 2", 2, 50m)
            });

        // Act:
        await service.AddInvoiceAsync(invoice, "tester");

        // Assert:
        var savedInvoice = await context.Invoices
            .Include(i => i.Items)
            .FirstAsync();

        Assert.Equal(patient.Id, savedInvoice.PatientId);
        Assert.Equal("RE-2001", savedInvoice.InvoiceNumber);

        // Gesamtbetrag = 100 + (2 * 50) = 200
        Assert.Equal(200m, savedInvoice.TotalAmount);

        auditMock.Verify(a => a.LogAsync(
                "tester",
                "CREATE",
                "Invoice",
                It.Is<string>(m => m.Contains("Rechnung RE-2001 erstellt"))),
            Times.Once);
    }

    [Fact]
    public async Task AddInvoiceAsync_ShouldGenerateInvoiceNumber_WhenEmpty()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new InvoiceService(context, auditMock.Object);

        var invoice = CreateInvoice(
            patient.Id,
            invoiceNumber: "",
            items: new List<InvoiceItem>
            {
                CreateInvoiceItem("Leistung", 1, 80m)
            });

        // Act:
        await service.AddInvoiceAsync(invoice, "tester");

        // Assert:
        var savedInvoice = await context.Invoices.FirstAsync();

        Assert.False(string.IsNullOrWhiteSpace(savedInvoice.InvoiceNumber));
        Assert.StartsWith("RE-", savedInvoice.InvoiceNumber);
        Assert.Equal(80m, savedInvoice.TotalAmount);
    }

    [Fact]
    public async Task DeleteInvoiceAsync_ShouldRemoveInvoice_AndWriteAuditLog()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var invoice = CreateInvoice(patient.Id, invoiceNumber: "RE-DELETE");
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        var auditMock = new Mock<IAuditService>();
        var service = new InvoiceService(context, auditMock.Object);

        // Act:
        await service.DeleteInvoiceAsync(invoice.Id, "tester");

        // Assert:
        Assert.Empty(context.Invoices);

        auditMock.Verify(a => a.LogAsync(
                "tester",
                "DELETE",
                "Invoice",
                It.Is<string>(m => m.Contains("Rechnung RE-DELETE gelöscht"))),
            Times.Once);
    }

    [Fact]
    public async Task DeleteInvoiceAsync_ShouldDoNothing_WhenInvoiceDoesNotExist()
    {
        // Arrange:
        using var context = CreateInMemoryContext();

        var auditMock = new Mock<IAuditService>();
        var service = new InvoiceService(context, auditMock.Object);

        // Act:
        await service.DeleteInvoiceAsync(999, "tester");

        // Assert:
        Assert.Empty(context.Invoices);

        auditMock.Verify(a => a.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }
}