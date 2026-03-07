using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;
using System.Linq;
using System.Threading.Tasks;
using System;
using Xunit;


public class AppointmentServiceTests
{
    // [Fact] markiert eine Methode als Unit-Test
    [Fact]
    public async Task AddAppointment_ShouldSaveAppointment()
    {
        // Erstellt eine InMemory-Datenbank für Tests
        // Dadurch brauchen wir keine echte SQLite-Datenbank
        var options = new DbContextOptionsBuilder<PraxisDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;

        // Erstellt eine Instanz des DbContext mit der InMemory-Datenbank
        using var context = new PraxisDbContext(options);

        // Erstellt den Service, der getestet werden soll
        var service = new AppointmentService(context);

        // Erstellt einen neuen Termin (Testdaten)
        var appointment = new Appointment
        {
            PatientId = 1,                 // ID des Patienten
            StartTime = DateTime.Now,      // Zeitpunkt des Termins
            DurationMinutes = 30,          // Dauer des Termins
            Reason = "Kontrolle",          // Grund für den Termin
            Status = "Geplant"             // Status des Termins
        };

        // Führt die Methode aus, die getestet werden soll
        // Diese Methode speichert den Termin in der Datenbank
        await service.AddAppointmentAsync(appointment);

        // Liest den gespeicherten Termin aus der Datenbank
        var count = context.Appointments.Count();

        Assert.Equal(1, count);

        // Liest den gespeicherten Termin aus der InMemory-Datenbank
        // FirstOrDefaultAsync() gibt den ersten Termin zurück oder null, wenn keiner existiert
        var savedAppointment = await context.Appointments.FirstOrDefaultAsync();

        // Prüft, ob überhaupt ein Termin gespeichert wurde
        // Wenn savedAppointment null ist, schlägt der Test fehl
        Assert.NotNull(savedAppointment);

        // Prüft, ob die PatientId korrekt gespeichert wurde
        Assert.Equal(1, savedAppointment.PatientId);

        // Prüft, ob der Termin-Grund korrekt gespeichert wurde
        Assert.Equal("Kontrolle", savedAppointment.Reason);

        // Prüft, ob der Status korrekt gespeichert wurde
        Assert.Equal("Geplant", savedAppointment.Status);

        // Prüft, ob die Dauer korrekt gespeichert wurde
        Assert.Equal(30, savedAppointment.DurationMinutes);
    }
}
