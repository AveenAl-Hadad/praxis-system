using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using Praxis.Infrastructure.Services.Interface;

namespace Praxis.Client.Views;

/// <summary>
/// Dieses Fenster zeigt alle Termine in einer Liste (DataGrid).
/// 
/// Funktionen:
/// - alle Termine anzeigen
/// - neue Termine erstellen
/// - bestehende Termine bearbeiten
/// - Termine löschen
/// - Erinnerungen per E-Mail senden
/// </summary>
public partial class AppointmentWindow : Window
{
    /// <summary>
    /// Service für Terminoperationen (laden, erstellen, löschen).
    /// </summary>
    private readonly IAppointmentService _appointmentService;

    /// <summary>
    /// Service zum Versenden von Erinnerungen (z. B. per E-Mail).
    /// </summary>
    private readonly IReminderService _reminderService;

    /// <summary>
    /// Konstruktor des Fensters.
    /// Initialisiert die Services und lädt beim Öffnen die Termine.
    /// </summary>
    public AppointmentWindow(IAppointmentService appointmentService, IReminderService reminderService)
    {
        InitializeComponent();
        _appointmentService = appointmentService;
        _reminderService = reminderService;

        Loaded += AppointmentWindow_Loaded;
    }

    /// <summary>
    /// Wird beim Laden des Fensters ausgeführt.
    /// Lädt alle Termine aus der Datenbank.
    /// </summary>
    private async void AppointmentWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAppointmentsAsync();
    }

    /// <summary>
    /// Lädt alle Termine und zeigt sie im DataGrid an.
    /// </summary>
    private async Task LoadAppointmentsAsync()
    {
        var appointments = await _appointmentService.GetAllAppointmentsAsync();
        AppointmentsDataGrid.ItemsSource = appointments;
    }

    /// <summary>
    /// Aktualisiert die Terminliste manuell.
    /// </summary>
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadAppointmentsAsync();
    }

    /// <summary>
    /// Öffnet das Fenster zum Erstellen eines neuen Termins.
    /// Nach erfolgreichem Speichern wird die Liste neu geladen.
    /// </summary>
    private async void NewAppointmentButton_Click(object sender, RoutedEventArgs e)
    {
        var addWindow = App.ServiceProvider.GetRequiredService<AddAppointmentWindow>();
        addWindow.Owner = this;

        var result = addWindow.ShowDialog();

        if (result == true)
        {
            await LoadAppointmentsAsync();
        }
    }

    /// <summary>
    /// Öffnet das Bearbeitungsfenster für den ausgewählten Termin.
    /// </summary>
    private async void EditAppointmentButton_Click(object sender, RoutedEventArgs e)
    {
        // Prüfen, ob ein Termin ausgewählt wurde
        if (AppointmentsDataGrid.SelectedItem is not Appointment selectedAppointment)
        {
            MessageBox.Show("Bitte zuerst einen Termin auswählen.");
            return;
        }

        // Termin aus der Datenbank neu laden
        var appointmentToEdit = await _appointmentService.GetAppointmentByIdAsync(selectedAppointment.Id);

        if (appointmentToEdit == null)
        {
            MessageBox.Show("Termin wurde nicht gefunden.");
            return;
        }

        // Bearbeitungsfenster öffnen
        var editWindow = App.ServiceProvider.GetRequiredService<AddAppointmentWindow>();
        editWindow.Owner = this;
        editWindow.SetAppointmentForEdit(appointmentToEdit);

        var result = editWindow.ShowDialog();

        // Nach dem Speichern Liste aktualisieren
        if (result == true)
        {
            await LoadAppointmentsAsync();
        }
    }

    /// <summary>
    /// Löscht den ausgewählten Termin nach Bestätigung durch den Benutzer.
    /// </summary>
    private async void DeleteAppointmentButton_Click(object sender, RoutedEventArgs e)
    {
        // Prüfen, ob ein Termin ausgewählt wurde
        if (AppointmentsDataGrid.SelectedItem is not Appointment selectedAppointment)
        {
            MessageBox.Show("Bitte zuerst einen Termin auswählen.");
            return;
        }

        // Sicherheitsabfrage
        var result = MessageBox.Show(
            "Möchten Sie diesen Termin wirklich löschen?",
            "Termin löschen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            // Termin löschen
            await _appointmentService.DeleteAppointmentAsync(selectedAppointment.Id);

            MessageBox.Show("Termin wurde gelöscht.");

            // Liste und Dashboard aktualisieren
            await LoadAppointmentsAsync();
            await ((MainWindow)Application.Current.MainWindow).LoadDashboardAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    /// <summary>
    /// Sendet eine Erinnerung für den ausgewählten Termin (z. B. per E-Mail).
    /// </summary>
    private async void SendReminder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Prüfen, ob ein Termin ausgewählt wurde
            if (AppointmentsDataGrid.SelectedItem is not Appointment appointment)
            {
                MessageBox.Show("Bitte zuerst einen Termin auswählen.");
                return;
            }

            // Erinnerung senden
            await _reminderService.SendAppointmentReminderAsync(appointment);

            MessageBox.Show("Erinnerung wurde gesendet.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"E-Mail konnte nicht gesendet werden:\n{ex.Message}");
        }
    }
}