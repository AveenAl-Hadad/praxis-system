using System.Windows;
using System.Windows.Controls;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using Praxis.Infrastructure.Services.Interface;

namespace Praxis.Client.Views;

/// <summary>
/// Dieses Fenster ermöglicht die Online-Terminbuchung.
/// 
/// Der Benutzer kann:
/// - einen Patienten auswählen
/// - ein Datum auswählen
/// - eine Termindauer festlegen
/// - freie Zeitfenster anzeigen lassen
/// - einen Termin verbindlich buchen
/// </summary>
public partial class OnlineBookingWindow : Window
{
    /// <summary>
    /// Service für Terminfunktionen.
    /// Wird verwendet, um freie Zeitfenster zu laden
    /// und neue Termine zu speichern.
    /// </summary>
    private readonly IAppointmentService _appointmentService;

    /// <summary>
    /// Service zum Laden der Patienten.
    /// </summary>
    private readonly IPatientService _patientService;

    /// <summary>
    /// Konstruktor des Fensters.
    /// 
    /// Übergibt die benötigten Services und registriert das Loaded-Event,
    /// damit beim Öffnen des Fensters direkt die Daten geladen werden.
    /// </summary>
    public OnlineBookingWindow(
        IAppointmentService appointmentService,
        IPatientService patientService)
    {
        InitializeComponent();
        _appointmentService = appointmentService;
        _patientService = patientService;

        Loaded += OnlineBookingWindow_Loaded;
    }

    /// <summary>
    /// Wird automatisch ausgeführt, wenn das Fenster geladen wurde.
    /// 
    /// Es werden:
    /// - alle Patienten geladen
    /// - das heutige Datum als Standard gesetzt
    /// - freie Termine für dieses Datum geladen
    /// </summary>
    private async void OnlineBookingWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadPatientsAsync();
        AppointmentDatePicker.SelectedDate = DateTime.Today;
        await LoadAvailableSlotsAsync();
    }

    /// <summary>
    /// Lädt alle Patienten und zeigt sie in der ComboBox an.
    /// </summary>
    private async Task LoadPatientsAsync()
    {
        var patients = await _patientService.GetAllPatientsAsync();
        PatientComboBox.ItemsSource = patients;
    }

    /// <summary>
    /// Lädt alle verfügbaren freien Termine für das ausgewählte Datum
    /// und die ausgewählte Dauer.
    /// 
    /// Dabei werden nur gültige Tage berücksichtigt:
    /// - kein leeres Datum
    /// - kein Datum in der Vergangenheit
    /// - keine Wochenenden
    /// </summary>
    private async Task LoadAvailableSlotsAsync()
    {
        // Alte Slots zuerst leeren
        SlotsListBox.ItemsSource = null;

        // Prüfen, ob ein Datum ausgewählt wurde
        if (AppointmentDatePicker.SelectedDate == null)
            return;

        var selectedDate = AppointmentDatePicker.SelectedDate.Value;

        // Keine Termine in der Vergangenheit anzeigen
        if (selectedDate.Date < DateTime.Today)
            return;

        // Samstag und Sonntag ausschließen
        if (selectedDate.DayOfWeek == DayOfWeek.Saturday || selectedDate.DayOfWeek == DayOfWeek.Sunday)
            return;

        var duration = GetSelectedDuration();
        var slots = await _appointmentService.GetAvailableSlotsAsync(selectedDate, duration);

        SlotsListBox.ItemsSource = slots;
    }

    /// <summary>
    /// Liest die im Dauer-ComboBox ausgewählte Termindauer aus.
    /// 
    /// Falls nichts ausgewählt oder die Eingabe ungültig ist,
    /// wird standardmäßig 15 Minuten zurückgegeben.
    /// </summary>
    private int GetSelectedDuration()
    {
        if (DurationComboBox.SelectedItem is ComboBoxItem item &&
            int.TryParse(item.Content?.ToString(), out var duration))
        {
            return duration;
        }

        return 15;
    }

    /// <summary>
    /// Wird aufgerufen, wenn sich das ausgewählte Datum ändert.
    /// Danach werden die freien Termine neu geladen.
    /// </summary>
    private async void AppointmentDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadAvailableSlotsAsync();
    }

    /// <summary>
    /// Wird aufgerufen, wenn sich die ausgewählte Termindauer ändert.
    /// Danach werden die freien Zeitfenster neu berechnet.
    /// </summary>
    private async void DurationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
        {
            await LoadAvailableSlotsAsync();
        }
    }

    /// <summary>
    /// Bricht die Buchung ab und schließt das Fenster ohne zu speichern.
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Führt die eigentliche Terminbuchung aus.
    /// 
    /// Ablauf:
    /// - prüft, ob ein Patient ausgewählt wurde
    /// - prüft, ob ein freier Termin ausgewählt wurde
    /// - prüft, ob Datum und Uhrzeit nicht in der Vergangenheit liegen
    /// - prüft, ob der Slot noch frei ist
    /// - zeigt eine Bestätigung an
    /// - erstellt und speichert den Termin
    /// </summary>
    private async void BookButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Prüfen, ob ein Patient ausgewählt wurde
            if (PatientComboBox.SelectedValue is not int patientId)
            {
                MessageBox.Show("Bitte einen Patienten auswählen.");
                return;
            }

            // Prüfen, ob ein freier Termin ausgewählt wurde
            if (SlotsListBox.SelectedItem is not DateTime selectedStartTime)
            {
                MessageBox.Show("Bitte einen freien Termin auswählen.");
                return;
            }

            var duration = GetSelectedDuration();

            // Prüfen, ob das Datum in der Vergangenheit liegt
            if (selectedStartTime.Date < DateTime.Today)
            {
                MessageBox.Show("Termine in der Vergangenheit können nicht gebucht werden.");
                return;
            }

            // Prüfen, ob die Uhrzeit bereits vergangen ist
            if (selectedStartTime < DateTime.Now)
            {
                MessageBox.Show("Vergangene Uhrzeiten können nicht gebucht werden.");
                return;
            }

            // Prüfen, ob der Slot noch frei ist
            var isAvailable = await _appointmentService.IsTimeSlotAvailableAsync(selectedStartTime, duration);
            if (!isAvailable)
            {
                MessageBox.Show("Der Termin wurde inzwischen vergeben. Bitte wähle einen anderen Slot.");
                await LoadAvailableSlotsAsync();
                return;
            }

            // Buchungszusammenfassung zur Bestätigung anzeigen
            var summary =
                $"Patient-ID: {patientId}\n" +
                $"Datum: {selectedStartTime:dd.MM.yyyy}\n" +
                $"Uhrzeit: {selectedStartTime:HH:mm}\n" +
                $"Dauer: {duration} Minuten\n\n" +
                $"Termin jetzt verbindlich buchen?";

            var confirm = MessageBox.Show(
                summary,
                "Termin bestätigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            // Terminobjekt erstellen
            var appointment = new Appointment
            {
                PatientId = patientId,
                StartTime = selectedStartTime,
                DurationMinutes = duration,
                Reason = "Online gebucht",
                Status = "Bestätigt"
            };

            // Termin speichern
            await _appointmentService.AddAppointmentAsync(appointment);

            MessageBox.Show("Termin wurde erfolgreich gebucht.");
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}