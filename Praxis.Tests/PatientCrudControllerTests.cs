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

public class PatientCrudControllerTests
{
    [Fact]
    public async Task AddAsync_WhenDialogReturnsOk_CallsAddPatientAsync()
    {
        // Arrange
        var patient = new Patient
        {
            Vorname = "Max",
            Nachname = "Mustermann",
            Geburtsdatum = new DateTime(1980, 1, 1),
            IsActive = true
        };

        var patientService = new Mock<IPatientService>();
        var dialogService = new Mock<IDialogService>();
        var messageBoxService = new Mock<IMessageBoxService>();

        dialogService
            .Setup(d => d.TryCreatePatient(It.IsAny<Window>(), out patient))
            .Returns(true);

        var controller = new PatientCrudController(
            patientService.Object,
            dialogService.Object,
            messageBoxService.Object);

        // Act
        var result = await controller.AddAsync(new Window());

        // Assert
        Assert.True(result);
        patientService.Verify(s => s.AddPatientAsync(It.Is<Patient>(p => p.Nachname == "Mustermann")), Times.Once);
        messageBoxService.Verify(m => m.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenDialogCancelled_DoesNotCallAddPatientAsync()
    {
        // Arrange
        Patient? patient = null;

        var patientService = new Mock<IPatientService>();
        var dialogService = new Mock<IDialogService>();
        var messageBoxService = new Mock<IMessageBoxService>();

        dialogService
            .Setup(d => d.TryCreatePatient(It.IsAny<Window>(), out patient))
            .Returns(false);

        var controller = new PatientCrudController(
            patientService.Object,
            dialogService.Object,
            messageBoxService.Object);

        // Act
        var result = await controller.AddAsync(new Window());

        // Assert
        Assert.False(result);
        patientService.Verify(s => s.AddPatientAsync(It.IsAny<Patient>()), Times.Never);
    }

    [Fact]
    public async Task EditAsync_WhenDialogReturnsOk_CallsUpdatePatientAsync()
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

        Patient? updated = new Patient
        {
            Id = 1,
            Vorname = "Neu",
            Nachname = "Name",
            Geburtsdatum = new DateTime(1980, 1, 1),
            IsActive = true
        };

        var patientService = new Mock<IPatientService>();
        var dialogService = new Mock<IDialogService>();
        var messageBoxService = new Mock<IMessageBoxService>();

        dialogService
            .Setup(d => d.TryEditPatient(It.IsAny<Window>(), selected, out updated))
            .Returns(true);

        var controller = new PatientCrudController(
            patientService.Object,
            dialogService.Object,
            messageBoxService.Object);

        // Act
        var result = await controller.EditAsync(new Window(), selected);

        // Assert
        Assert.True(result);
        patientService.Verify(s => s.UpdatePatientAsync(It.Is<Patient>(p => p.Id == 1 && p.Vorname == "Neu")), Times.Once);
    }

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

        var patientService = new Mock<IPatientService>();
        var dialogService = new Mock<IDialogService>();
        var messageBoxService = new Mock<IMessageBoxService>();

        dialogService
            .Setup(d => d.TryEditPatient(It.IsAny<Window>(), selected, out updated))
            .Returns(false);

        var controller = new PatientCrudController(
            patientService.Object,
            dialogService.Object,
            messageBoxService.Object);

        // Act
        var result = await controller.EditAsync(new Window(), selected);

        // Assert
        Assert.False(result);
        patientService.Verify(s => s.UpdatePatientAsync(It.IsAny<Patient>()), Times.Never);
    }

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

        messageBoxService
            .Setup(m => m.Confirm(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(MessageBoxResult.No);

        var controller = new PatientCrudController(
            patientService.Object,
            dialogService.Object,
            messageBoxService.Object);

        // Act
        var result = await controller.DeleteAsync(selected);

        // Assert
        Assert.False(result);
        patientService.Verify(s => s.DeletePatientAsync(It.IsAny<int>()), Times.Never);
    }

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
        var result = await controller.DeleteAsync(selected);

        // Assert
        Assert.True(result);
        patientService.Verify(s => s.DeletePatientAsync(10), Times.Once);
    }

    [Fact]
    public async Task ToggleActiveAsync_CallsToggleActiveAsync()
    {
        // Arrange
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
        Assert.True(result);
        patientService.Verify(s => s.ToggleActiveAsync(99), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenServiceThrows_ShowsErrorAndReturnsFalse()
    {
        // Arrange
        var patient = new Patient
        {
            Vorname = "Max",
            Nachname = "Mustermann",
            Geburtsdatum = new DateTime(1980, 1, 1),
            IsActive = true
        };

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
        var result = await controller.AddAsync(new Window());

        // Assert
        Assert.False(result);
        messageBoxService.Verify(m => m.ShowError("DB Fehler", "Fehler"), Times.Once);
    }
}