using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;
using Xunit;

namespace Praxis.Tests;

public class AppointmentServiceTests
{
    // Erstellt eine InMemory-Datenbank für jeden Test.
    // So hat jeder Test seine eigene "kleine Test-Datenbank".
    private static PraxisDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<PraxisDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new PraxisDbContext(options);
    }

    // Hilfsmethode: Erstellt einen Beispiel-Patienten.
    // Diesen brauchen wir in vielen Tests, weil Appointments zu einem Patient gehören.
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

    // Hilfsmethode: Erstellt einen Beispiel-Termin.
    // Damit müssen wir in jedem Test nicht alles neu ausschreiben.
    private static Appointment CreateAppointment(
        int patientId = 1,
        DateTime? startTime = null,
        int durationMinutes = 30,
        string reason = "Kontrolle",
        string status = "Geplant")
    {
        return new Appointment
        {
            PatientId = patientId,
            StartTime = startTime ?? DateTime.Today.AddHours(9),
            DurationMinutes = durationMinutes,
            Reason = reason,
            Status = status
        };
    }

    [Fact]
    public async Task AddAppointmentAsync_ShouldSaveAppointment()
    {
        // Arrange = Test vorbereiten
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);

        // Wir erstellen einen Termin
        var appointment = CreateAppointment();

        // Act = Methode aufrufen
        await service.AddAppointmentAsync(appointment);

        // Assert = Prüfen, ob der Termin wirklich gespeichert wurde
        var saved = await context.Appointments.SingleAsync();

        Assert.Equal(1, saved.PatientId);
        Assert.Equal("Kontrolle", saved.Reason);
        Assert.Equal("Geplant", saved.Status);
        Assert.Equal(30, saved.DurationMinutes);
    }

    [Fact]
    public async Task AddAppointmentAsync_ShouldThrowException_WhenConflictExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);

        // Bereits vorhandener Termin von 10:00 bis 10:30
        context.Appointments.Add(CreateAppointment(
            patientId: 1,
            startTime: DateTime.Today.AddHours(10),
            durationMinutes: 30,
            reason: "Bestehend"));
        await context.SaveChangesAsync();

        // Neuer Termin überschneidet sich mit dem bestehenden Termin
        var conflicting = CreateAppointment(
            patientId: 2,
            startTime: DateTime.Today.AddHours(10).AddMinutes(15),
            durationMinutes: 30,
            reason: "Konflikt");

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddAppointmentAsync(conflicting));

        Assert.Equal("Es existiert bereits ein Termin in diesem Zeitraum.", ex.Message);
    }

    [Fact]
    public async Task GetAllAppointmentsAsync_ShouldReturnAppointmentsOrderedByStartTime()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);

        // Patient anlegen, weil Appointment einen PatientId-Bezug hat
        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        // Drei Termine in unsortierter Reihenfolge speichern
        context.Appointments.AddRange(
            CreateAppointment(patientId: patient.Id, startTime: DateTime.Today.AddHours(11), reason: "Später"),
            CreateAppointment(patientId: patient.Id, startTime: DateTime.Today.AddHours(8), reason: "Früher"),
            CreateAppointment(patientId: patient.Id, startTime: DateTime.Today.AddHours(9), reason: "Mitte"));
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllAppointmentsAsync();

        // Assert: Service soll nach StartTime sortieren
        Assert.Equal(3, result.Count);
        Assert.Equal("Früher", result[0].Reason);
        Assert.Equal("Mitte", result[1].Reason);
        Assert.Equal("Später", result[2].Reason);
    }

    [Fact]
    public async Task GetAppointmentsByDateAsync_ShouldReturnOnlyAppointmentsOfGivenDay()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);
        var targetDate = new DateTime(2026, 3, 18);

        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        // Zwei Termine am Zieltag, einer am nächsten Tag
        context.Appointments.AddRange(
            CreateAppointment(patientId: patient.Id, startTime: targetDate.AddHours(9), reason: "Heute 1"),
            CreateAppointment(patientId: patient.Id, startTime: targetDate.AddHours(14), reason: "Heute 2"),
            CreateAppointment(patientId: patient.Id, startTime: targetDate.AddDays(1).AddHours(9), reason: "Morgen"));
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAppointmentsByDateAsync(targetDate);

        // Assert: Nur Termine vom gewünschten Datum sollen zurückkommen
        Assert.Equal(2, result.Count);
        Assert.Equal("Heute 1", result[0].Reason);
        Assert.Equal("Heute 2", result[1].Reason);
        Assert.All(result, a => Assert.Equal(targetDate.Date, a.StartTime.Date));
    }

    [Fact]
    public async Task GetAppointmentsByWeekAsync_ShouldReturnOnlyAppointmentsInSevenDayRange()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);
        var weekStart = new DateTime(2026, 3, 16);

        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        // Zwei Termine in der Woche, einer genau 7 Tage später -> soll NICHT dabei sein
        context.Appointments.AddRange(
            CreateAppointment(patientId: patient.Id, startTime: weekStart.AddHours(8), reason: "Montag"),
            CreateAppointment(patientId: patient.Id, startTime: weekStart.AddDays(3).AddHours(9), reason: "Donnerstag"),
            CreateAppointment(patientId: patient.Id, startTime: weekStart.AddDays(7).AddHours(10), reason: "Nächste Woche"));
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAppointmentsByWeekAsync(weekStart);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Montag", result[0].Reason);
        Assert.Equal("Donnerstag", result[1].Reason);
        Assert.DoesNotContain(result, a => a.Reason == "Nächste Woche");
    }

    [Fact]
    public async Task GetAppointmentsByWeekAndPatientAsync_ShouldFilterByWeekAndPatient()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);
        var weekStart = new DateTime(2026, 3, 16);

        // Zwei echte Patienten anlegen
        var patient1 = CreatePatient("Max", "Mustermann", "max1@test.de", "111");
        var patient2 = CreatePatient("Anna", "Meyer", "anna@test.de", "222");

        context.Patients.AddRange(patient1, patient2);
        await context.SaveChangesAsync();

        // Ein Termin von Patient1 in der Woche
        // Ein Termin von Patient2 in der Woche
        // Ein Termin von Patient1 außerhalb der Woche
        context.Appointments.AddRange(
            CreateAppointment(patientId: patient1.Id, startTime: weekStart.AddHours(9), reason: "P1"),
            CreateAppointment(patientId: patient2.Id, startTime: weekStart.AddDays(1).AddHours(9), reason: "P2"),
            CreateAppointment(patientId: patient1.Id, startTime: weekStart.AddDays(8).AddHours(9), reason: "Außerhalb"));
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAppointmentsByWeekAndPatientAsync(weekStart, patient1.Id);

        // Assert: Es soll nur der eine Termin von Patient1 in dieser Woche kommen
        var item = Assert.Single(result);
        Assert.Equal(patient1.Id, item.PatientId);
        Assert.Equal("P1", item.Reason);
    }

    [Fact]
    public async Task GetAppointmentByIdAsync_ShouldReturnMatchingAppointment()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);

        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var appointment = CreateAppointment(
            patientId: patient.Id,
            reason: "Detailtest");

        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var appointmentId = appointment.Id;

        // Act
        var result = await service.GetAppointmentByIdAsync(appointmentId);

        // Assert: Termin mit der richtigen ID muss zurückkommen
        Assert.NotNull(result);
        Assert.Equal(appointmentId, result!.Id);
        Assert.Equal("Detailtest", result.Reason);
        Assert.Equal(patient.Id, result.PatientId);
    }

    [Fact]
    public async Task GetWaitingRoomAppointmentsAsync_ShouldExcludeCancelledAndCompletedAppointments()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);
        var date = new DateTime(2026, 3, 18);

        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        // Nur der erste Termin soll zurückkommen:
        // - "Geplant" = erlaubt
        // - "Abgesagt" = muss raus
        // - "Erledigt" = muss raus
        // - anderer Tag = muss raus
        context.Appointments.AddRange(
            CreateAppointment(patientId: patient.Id, startTime: date.AddHours(8), reason: "Erwartet", status: "Geplant"),
            CreateAppointment(patientId: patient.Id, startTime: date.AddHours(9), reason: "Abgesagt", status: "Abgesagt"),
            CreateAppointment(patientId: patient.Id, startTime: date.AddHours(10), reason: "Erledigt", status: "Erledigt"),
            CreateAppointment(patientId: patient.Id, startTime: date.AddDays(1).AddHours(8), reason: "Anderer Tag", status: "Geplant"));
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetWaitingRoomAppointmentsAsync(date);

        // Assert
        var item = Assert.Single(result);
        Assert.Equal("Erwartet", item.Reason);
        Assert.Equal("Geplant", item.Status);
    }

    [Fact]
    public async Task UpdateAppointmentAsync_ShouldModifyExistingAppointment()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);

        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        // Ursprünglichen Termin speichern
        var appointment = CreateAppointment(
            patientId: patient.Id,
            reason: "Alt",
            durationMinutes: 30,
            status: "Geplant");

        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        // Alte Zeit separat speichern, damit wir später sauber vergleichen können
        var oldStartTime = appointment.StartTime;

        // Neues Objekt mit geänderten Daten
        var updatedAppointment = new Appointment
        {
            Id = appointment.Id,
            PatientId = patient.Id,
            StartTime = oldStartTime.AddHours(1),
            DurationMinutes = 45,
            Reason = "Neu",
            Status = "Bestätigt"
        };

        // Act
        await service.UpdateAppointmentAsync(updatedAppointment);

        // Assert: Prüfen, ob die Änderungen in DB übernommen wurden
        var saved = await context.Appointments.SingleAsync();
        Assert.Equal("Neu", saved.Reason);
        Assert.Equal(45, saved.DurationMinutes);
        Assert.Equal("Bestätigt", saved.Status);
        Assert.Equal(oldStartTime.AddHours(1), saved.StartTime);
    }

    [Fact]
    public async Task UpdateAppointmentStatusAsync_ShouldChangeStatus()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);

        var appointment = CreateAppointment(status: "Geplant");
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateAppointmentStatusAsync(appointment.Id, "Im Wartezimmer");

        // Assert
        var updated = await context.Appointments.SingleAsync();
        Assert.Equal("Im Wartezimmer", updated.Status);
    }

    [Fact]
    public async Task DeleteAppointmentAsync_ShouldRemoveAppointment()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);

        var appointment = CreateAppointment();
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        // Act
        await service.DeleteAppointmentAsync(appointment.Id);

        // Assert: Es darf kein Termin mehr vorhanden sein
        Assert.Empty(context.Appointments);
    }

    [Fact]
    public async Task IsTimeSlotAvailableAsync_ShouldReturnFalse_WhenTimeSlotOverlaps()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);

        // Termin von 10:00 bis 10:30
        context.Appointments.Add(CreateAppointment(
            startTime: DateTime.Today.AddHours(10),
            durationMinutes: 30,
            reason: "Belegt"));
        await context.SaveChangesAsync();

        // Act: neuer Slot überschneidet sich (10:15 bis 10:45)
        var result = await service.IsTimeSlotAvailableAsync(
            DateTime.Today.AddHours(10).AddMinutes(15),
            30);

        // Assert: muss false sein
        Assert.False(result);
    }

    [Fact]
    public async Task IsTimeSlotAvailableAsync_ShouldReturnTrue_WhenTimeSlotDoesNotOverlap()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);

        // Termin von 10:00 bis 10:30
        context.Appointments.Add(CreateAppointment(
            startTime: DateTime.Today.AddHours(10),
            durationMinutes: 30,
            reason: "Belegt"));
        await context.SaveChangesAsync();

        // Act: neuer Slot ist erst um 11:00 -> keine Überschneidung
        var result = await service.IsTimeSlotAvailableAsync(
            DateTime.Today.AddHours(11),
            30);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ShouldReturnEmptyList_ForPastDate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);

        // Act: Für vergangene Tage sollen keine freien Slots kommen
        var result = await service.GetAvailableSlotsAsync(DateTime.Today.AddDays(-1), 30);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ShouldReturnWorkingDaySlots_ForFutureWednesday()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);

        // Suche den nächsten Mittwoch in der Zukunft
        var futureWednesday = NextDayOfWeek(DateTime.Today.AddDays(1), DayOfWeek.Wednesday);

        // Act
        var result = await service.GetAvailableSlotsAsync(futureWednesday, 30);

        // Assert:
        // Mittwoch hat laut Service Arbeitszeit von 08:00 bis 12:00
        Assert.NotEmpty(result);
        Assert.All(result, slot => Assert.Equal(futureWednesday.Date, slot.Date));
        Assert.Equal(futureWednesday.Date.AddHours(8), result.First());
        Assert.Contains(futureWednesday.Date.AddHours(11).AddMinutes(30), result);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ShouldExcludeConflictingSlots()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);

        // Nächsten Montag suchen
        var futureMonday = NextDayOfWeek(DateTime.Today.AddDays(1), DayOfWeek.Monday);

        // Termin belegt den Slot 08:00 bis 08:30
        context.Appointments.Add(CreateAppointment(
            startTime: futureMonday.Date.AddHours(8),
            durationMinutes: 30,
            reason: "Belegt"));
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAvailableSlotsAsync(futureMonday, 30);

        // Assert:
        // 08:00 darf nicht verfügbar sein
        // 08:30 darf verfügbar sein
        Assert.DoesNotContain(futureMonday.Date.AddHours(8), result);
        Assert.Contains(futureMonday.Date.AddHours(8).AddMinutes(30), result);
    }

    [Fact]
    public async Task UpdateAppointmentAsync_ShouldThrow_WhenAppointmentDoesNotExist()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);

        // Termin mit ID 999 existiert nicht in DB
        var appointment = new Appointment
        {
            Id = 999,
            PatientId = 1,
            StartTime = DateTime.Today.AddHours(9),
            DurationMinutes = 30,
            Reason = "Nicht vorhanden",
            Status = "Geplant"
        };

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAppointmentAsync(appointment));

        Assert.Equal("Termin wurde nicht gefunden.", ex.Message);
    }

    [Fact]
    public async Task UpdateAppointmentStatusAsync_ShouldThrow_WhenStatusIsEmpty()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);

        // Act + Assert: Leerzeichen als Status sind ungültig
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateAppointmentStatusAsync(1, "   "));

        Assert.Equal("Status darf nicht leer sein.", ex.Message);
    }

    [Fact]
    public async Task DeleteAppointmentAsync_ShouldThrow_WhenAppointmentDoesNotExist()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AppointmentService(context);

        // Act + Assert: Löschen eines nicht existierenden Termins
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAppointmentAsync(404));

        Assert.Equal("Termin wurde nicht gefunden.", ex.Message);
    }

    // Hilfsmethode:
    // Findet ab einem Startdatum den nächsten gewünschten Wochentag.
    // Beispiel: Nächsten Mittwoch oder nächsten Montag.
    private static DateTime NextDayOfWeek(DateTime start, DayOfWeek dayOfWeek)
    {
        var date = start.Date;
        while (date.DayOfWeek != dayOfWeek)
        {
            date = date.AddDays(1);
        }

        return date;
    }
}