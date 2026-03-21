using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;
using Xunit;

namespace Praxis.Tests;

public class DashboardServiceTests
{
    // InMemory-Datenbank erstellen
    private static PraxisDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<PraxisDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new PraxisDbContext(options);
    }

    [Fact]
    public async Task GetStatsAsync_ShouldReturnCorrectCountsAndRevenue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new DashboardService(context);

        var now = DateTime.Now;
        var thisMonth = new DateTime(now.Year, now.Month, 1);
        var nextMonth = thisMonth.AddMonths(1);

        // Patienten hinzufügen
        context.Patients.AddRange(
            new Patient { Vorname = "Max", Nachname = "A", Email = "a@test.de", Telefonnummer = "1", Geburtsdatum = DateTime.Now },
            new Patient { Vorname = "Anna", Nachname = "B", Email = "b@test.de", Telefonnummer = "2", Geburtsdatum = DateTime.Now }
        );

        // Termine hinzufügen (1 im Monat, 1 außerhalb)
        context.Appointments.AddRange(
            new Appointment { PatientId = 1, StartTime = thisMonth.AddDays(1), DurationMinutes = 30, Reason = "Test", Status = "Geplant" },
            new Appointment { PatientId = 1, StartTime = nextMonth.AddDays(1), DurationMinutes = 30, Reason = "Test", Status = "Geplant" }
        );

        // Rechnungen hinzufügen (1 im Monat, 1 außerhalb)
        context.Invoices.AddRange(
            new Invoice { InvoiceDate = thisMonth.AddDays(2), TotalAmount = 100 },
            new Invoice { InvoiceDate = nextMonth.AddDays(2), TotalAmount = 200 }
        );

        // Rezepte hinzufügen
        context.Prescriptions.Add(
            new Prescription { PatientId = 1, MedicationName = "Test", IssueDate = DateTime.Now }
        );

        await context.SaveChangesAsync();

        // Act
        var stats = await service.GetStatsAsync();

        // Assert

        // Gesamtwerte prüfen
        Assert.Equal(2, stats.TotalPatients);
        Assert.Equal(2, stats.TotalAppointments);
        Assert.Equal(2, stats.TotalInvoices);
        Assert.Equal(1, stats.TotalPrescriptions);

        // Gesamtumsatz (100 + 200)
        Assert.Equal(300, stats.TotalRevenue);

        // Monatswerte prüfen
        Assert.Equal(1, stats.CurrentMonthAppointments);
        Assert.Equal(1, stats.CurrentMonthInvoices);
        Assert.Equal(100, stats.CurrentMonthRevenue);
    }
}