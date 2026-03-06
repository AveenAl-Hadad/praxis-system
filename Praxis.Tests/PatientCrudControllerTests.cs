using System;
using System.Threading.Tasks;
using System.Windows;
using Moq;
using Praxis.Client.Logic;
using Praxis.Client.Logic.UI;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using Xunit;

namespace Praxis.Tests;
/*
 * Diese Datei testet:

✔ Create (Add)
✔ Update (Edit)
✔ Delete
✔ Toggle Active
✔ Error Handling

 */
public class PatientCrudControllerTests
{
    /// <summary>
    /// Dieser Test prüft:

    ///➡️ Wenn der Benutzer im Dialog auf OK klickt, dann wird der Patient gespeichert.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task AddAsync_WhenDialogReturnsOk_CallsAddPatientAsync()
    {
        // Arrange
        //Ein Patient wird erstellt
        var patient = new Patient
        {
            Vorname = "Max",
            Nachname = "Mustermann",
            Geburtsdatum = new DateTime(1980, 1, 1),
            IsActive = true
        };
        
        Window? owner = null;
        //Der Dialog wird simuliert (Mock)
        var patientService = new Mock<IPatientService>();
        var dialogService = new Mock<IDialogService>();
        var messageBoxService = new Mock<IMessageBoxService>();

        //Der Dialog gibt true zurück (Benutzer klickt OK)
        dialogService
            .Setup(d => d.TryCreatePatient(It.IsAny<Window>(), out patient))
            .Returns(true);

        var controller = new PatientCrudController(
            patientService.Object,
            dialogService.Object,
            messageBoxService.Object);

        // Act
        //AddAsync() wird aufgerufen
        var result = await controller.AddAsync(owner!);

        // Assert
        /* Der Test prüft:
         *  ✔ AddPatientAsync() wurde 1x aufgerufen
         *  ✔ Ergebnis ist true
        */
        Assert.True(result);
        patientService.Verify(s => s.AddPatientAsync(It.Is<Patient>(p => p.Nachname == "Mustermann")), Times.Once);
        messageBoxService.Verify(m => m.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
    /// <summary>
    /// Was wird getestet?
    ///➡️ Wenn der Benutzer Abbrechen klickt, darf kein Patient gespeichert werden.
    /// </summary>
    /// <returns></returns>

    [Fact]
    public async Task AddAsync_WhenDialogCancelled_DoesNotCallAddPatientAsync()
    {
        // Arrange
        //Dialog wird simuliert
        Patient? patient = null;
        Window? owner = null;

        var patientService = new Mock<IPatientService>();
        var dialogService = new Mock<IDialogService>();
        var messageBoxService = new Mock<IMessageBoxService>();

        //Dialog gibt false zurück
        dialogService
            .Setup(d => d.TryCreatePatient(It.IsAny<Window>(), out patient))
            .Returns(false);

        var controller = new PatientCrudController(
            patientService.Object,
            dialogService.Object,
            messageBoxService.Object);

        // Act
        //AddAsync() wird aufgerufen
        var result = await controller.AddAsync(owner!);

        // Assert
        /* Erwartung
         * ✔ Ergebnis = false
         * ✔ AddPatientAsync() wurde nie aufgerufen 
         */
        Assert.False(result);
        patientService.Verify(s => s.AddPatientAsync(It.IsAny<Patient>()), Times.Never);
    }
    /// <summary>
    /// Was wird getestet?
    ///➡️ Wenn ein Patient bearbeitet und bestätigt wird, dann wird er aktualisiert.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task EditAsync_WhenDialogReturnsOk_CallsUpdatePatientAsync()
    {
        // Arrange
        //Ein alter Patient (selected)
        var selected = new Patient
        {
            Id = 1,
            Vorname = "Alt",
            Nachname = "Name",
            Geburtsdatum = new DateTime(1980, 1, 1),
            IsActive = true
        };

        //Ein neuer Patient (updated)
        Patient? updated = new Patient
        {
            Id = 1,
            Vorname = "Neu",
            Nachname = "Name",
            Geburtsdatum = new DateTime(1980, 1, 1),
            IsActive = true
        };

        Window? owner = null;
        var patientService = new Mock<IPatientService>();
        var dialogService = new Mock<IDialogService>();
        var messageBoxService = new Mock<IMessageBoxService>();

        //Dialog simuliert OK
        dialogService
            .Setup(d => d.TryEditPatient(It.IsAny<Window>(), selected, out updated))
            .Returns(true);

        var controller = new PatientCrudController(
            patientService.Object,
            dialogService.Object,
            messageBoxService.Object);

        // Act
        //EditAsync() wird aufgerufen
        var result = await controller.EditAsync(owner!, selected);

        // Assert
        /* Erwartung
         * ✔ UpdatePatientAsync() wird aufgerufen
         * ✔ Ergebnis = true 
         */
        Assert.True(result);
        patientService.Verify(s => s.UpdatePatientAsync(It.Is<Patient>(p => p.Id == 1 && p.Vorname == "Neu")), Times.Once);
    }

    /// <summary>
    /// Was wird getestet?
    ///➡️ Wenn Bearbeiten abgebrochen wird, darf kein Update passieren.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task EditAsync_WhenDialogCancelled_DoesNotCallUpdatePatientAsync()
    {
        // Arrange
        var selected = new Patient
        {
            Id = 1,
            Vorname = "Alt",
            Nachname = "Name",
            Geburtsdatum = new DateTime(1980, 1, 1),
            IsActive = true
        };

        Patient? updated = null;
        Window? owner = null;

        var patientService = new Mock<IPatientService>();
        var dialogService = new Mock<IDialogService>();
        var messageBoxService = new Mock<IMessageBoxService>();

        //Dialog gibt false zurück.
        dialogService
            .Setup(d => d.TryEditPatient(It.IsAny<Window>(), selected, out updated))
            .Returns(false);

        var controller = new PatientCrudController(
            patientService.Object,
            dialogService.Object,
            messageBoxService.Object);

        // Act
        var result = await controller.EditAsync(owner!, selected);

        // Assert
        /*Erwartung
         * ✔ Ergebnis = false
         * ✔ UpdatePatientAsync() wurde nicht aufgerufen
         */
        Assert.False(result);
        patientService.Verify(s => s.UpdatePatientAsync(It.IsAny<Patient>()), Times.Never);
    }
    /// <summary>
    /// Was wird getestet?
    ///➡️ Wenn der Benutzer beim Löschen Nein klickt.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task DeleteAsync_WhenConfirmNo_DoesNotCallDeletePatientAsync()
    {
    // Arrange
        var selected = new Patient
        {
            Id = 10,
            Vorname = "Sara",
            Nachname = "Müller"
        };

        var patientService = new Mock<IPatientService>();
        var dialogService = new Mock<IDialogService>();
        var messageBoxService = new Mock<IMessageBoxService>();
            //Der Bestätigungsdialog:
            messageBoxService
                .Setup(m => m.Confirm(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(MessageBoxResult.No);

        var controller = new PatientCrudController(
            patientService.Object,
            dialogService.Object,
            messageBoxService.Object);

        // Act
        //gibt No zurück.
        var result = await controller.DeleteAsync(selected);

        // Assert
        /*Erwartung
         * ✔ Patient wird nicht gelöscht
         */
        Assert.False(result);
        patientService.Verify(s => s.DeletePatientAsync(It.IsAny<int>()), Times.Never);
    }
    /// <summary>
    /// Was wird getestet?

    ///➡️ Wenn der Benutzer beim Löschen Ja klickt
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task DeleteAsync_WhenConfirmYes_CallsDeletePatientAsync()
        {
        // Arrange
        var selected = new Patient
        {
            Id = 10,
            Vorname = "Sara",
            Nachname = "Müller"
        };

        var patientService = new Mock<IPatientService>();
        var dialogService = new Mock<IDialogService>();
        var messageBoxService = new Mock<IMessageBoxService>();

        messageBoxService
            .Setup(m => m.Confirm(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(MessageBoxResult.Yes);

        var controller = new PatientCrudController(
            patientService.Object,
            dialogService.Object,
            messageBoxService.Object);

        // Act
        //Confirm() → Yes
        var result = await controller.DeleteAsync(selected);

        // Assert
        /*Erwartung
         * ✔ DeletePatientAsync() wird aufgerufen
         * ✔ Ergebnis = true
         */
        Assert.True(result);
        patientService.Verify(s => s.DeletePatientAsync(10), Times.Once);
    }
    /// <summary>
    /// Was wird getestet?

    ///➡️ Patient wird aktiv oder inaktiv geschaltet.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task ToggleActiveAsync_CallsToggleActiveAsync()
    {
        // Arrange
        //ToggleActiveAsync(selected)
        var selected = new Patient
        {
            Id = 99,
            Vorname = "Alex",
            Nachname = "Test"
        };

        var patientService = new Mock<IPatientService>();
        var dialogService = new Mock<IDialogService>();
        var messageBoxService = new Mock<IMessageBoxService>();

        var controller = new PatientCrudController(
            patientService.Object,
            dialogService.Object,
            messageBoxService.Object);

        // Act
        var result = await controller.ToggleActiveAsync(selected);

        // Assert
        //Erwartung: ✔ ToggleActiveAsync(id) wird aufgerufen
        Assert.True(result);
        patientService.Verify(s => s.ToggleActiveAsync(99), Times.Once);
    }
    /// <summary>
    /// Was wird getestet?
    /// ➡️ Fehlerbehandlung
    /// Wenn beim Speichern ein Fehler passiert(z.B.Datenbankfehler).
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task AddAsync_WhenServiceThrows_ShowsErrorAndReturnsFalse()
    {
        // Arrange
        /*Der Service wirft eine Exception:
         * ThrowsAsync(new Exception("DB Fehler"))
         */
        var patient = new Patient
        {
            Vorname = "Max",
            Nachname = "Mustermann",
            Geburtsdatum = new DateTime(1980, 1, 1),
            IsActive = true
        };

        Window? owner = null;
        var patientService = new Mock<IPatientService>();
        var dialogService = new Mock<IDialogService>();
        var messageBoxService = new Mock<IMessageBoxService>();

        dialogService
            .Setup(d => d.TryCreatePatient(It.IsAny<Window>(), out patient))
            .Returns(true);

        patientService
            .Setup(s => s.AddPatientAsync(It.IsAny<Patient>()))
            .ThrowsAsync(new Exception("DB Fehler"));

        var controller = new PatientCrudController(
            patientService.Object,
            dialogService.Object,
            messageBoxService.Object);

        // Act
        var result = await controller.AddAsync(owner!);

        // Assert
        /*Erwartung:
         * ✔ Methode gibt false zurück
         * ✔ Fehlermeldung wird angezeigt
         * ShowError("DB Fehler", "Fehler")
         */
        Assert.False(result);
        messageBoxService.Verify(m => m.ShowError("DB Fehler", "Fehler"), Times.Once);
    }
    }