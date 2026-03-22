using System.Windows;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services.Interface;

namespace Praxis.Client.Views;

/// <summary>
/// Dieses Fenster zeigt das Wartezimmer der Praxis.
/// 
/// Hier werden alle heutigen Termine angezeigt und deren Status kann geändert werden:
/// - Im Wartezimmer
/// - In Behandlung
/// - Erledigt
/// - Nicht erschienen
/// </summary>
public partial class WaitingRoomWindow : Window
{
    /// <summary>
    /// Service für Terminverwaltung.
    /// Wird verwendet, um Wartezimmer-Daten zu laden
    /// und den Status von Terminen zu ändern.
    /// </summary>
    private readonly IAppointmentService _appointmentService;

    /// <summary>
    /// Konstruktor des Fensters.
    /// 
    /// Übergibt den AppointmentService und lädt beim Öffnen automatisch
    /// die Wartezimmer-Daten.
    /// </summary>
    public WaitingRoomWindow(IAppointmentService appointmentService)
    {
        InitializeComponent();
        _appointmentService = appointmentService;
        Loaded += WaitingRoomWindow_Loaded;
    }

    /// <summary>
    /// Wird beim Laden des Fensters ausgeführt.
    /// 
    /// Lädt alle Termine für das heutige Wartezimmer.
    /// </summary>
    private async void WaitingRoomWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadWaitingRoomAsync();
    }

    /// <summary>
    /// Lädt alle Termine für den heutigen Tag,
    /// die im Wartezimmer angezeigt werden sollen.
    /// 
    /// Zusätzlich wird eine Info-Anzeige aktualisiert.
    /// </summary>
    private async Task LoadWaitingRoomAsync()
    {
        var appointments = await _appointmentService.GetWaitingRoomAppointmentsAsync(DateTime.Today);

        WaitingRoomDataGrid.ItemsSource = appointments;

        InfoTextBlock.Text =
            $"Wartezimmer für {DateTime.Today:dd.MM.yyyy} – {appointments.Count} Einträge";
    }

    /// <summary>
    /// Ändert den Status des ausgewählten Termins.
    /// 
    /// Nach der Änderung wird die Liste neu geladen.
    /// </summary>
    /// <param name="newStatus">Der neue Status des Termins.</param>
    private async Task UpdateSelectedStatusAsync(string newStatus)
    {
        // Prüfen, ob ein Termin ausgewählt wurde
        if (WaitingRoomDataGrid.SelectedItem is not Appointment selectedAppointment)
        {
            MessageBox.Show("Bitte zuerst einen Termin auswählen.");
            return;
        }

        try
        {
            // Status aktualisieren
            await _appointmentService.UpdateAppointmentStatusAsync(
                selectedAppointment.Id,
                newStatus);

            // Liste neu laden
            await LoadWaitingRoomAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Aktualisiert die Wartezimmerliste manuell.
    /// </summary>
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadWaitingRoomAsync();
    }

    /// <summary>
    /// Setzt den Status des Termins auf "Im Wartezimmer".
    /// </summary>
    private async void CheckInButton_Click(object sender, RoutedEventArgs e)
    {
        await UpdateSelectedStatusAsync("Im Wartezimmer");
    }

    /// <summary>
    /// Setzt den Status des Termins auf "In Behandlung".
    /// </summary>
    private async void StartTreatmentButton_Click(object sender, RoutedEventArgs e)
    {
        await UpdateSelectedStatusAsync("In Behandlung");
    }

    /// <summary>
    /// Setzt den Status des Termins auf "Erledigt".
    /// </summary>
    private async void CompleteButton_Click(object sender, RoutedEventArgs e)
    {
        await UpdateSelectedStatusAsync("Erledigt");
    }

    /// <summary>
    /// Setzt den Status des Termins auf "Nicht erschienen".
    /// </summary>
    private async void NoShowButton_Click(object sender, RoutedEventArgs e)
    {
        await UpdateSelectedStatusAsync("Nicht erschienen");
    }
}