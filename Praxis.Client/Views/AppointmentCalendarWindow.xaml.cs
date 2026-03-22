using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using Praxis.Infrastructure.Services.Interface;

namespace Praxis.Client.Views;

/// <summary>
/// Dieses Fenster zeigt alle Termine in einer Wochenübersicht.
/// 
/// Funktionen:
/// - Termine nach Woche anzeigen
/// - nach Patient filtern
/// - Termine bearbeiten (Doppelklick oder Button)
/// - Termine löschen
/// - Termine aktualisieren
/// </summary>
public partial class AppointmentCalendarWindow : Window
{
    /// <summary>
    /// Service für Terminoperationen (laden, löschen, bearbeiten).
    /// </summary>
    private readonly IAppointmentService _appointmentService;

    /// <summary>
    /// Service zum Laden der Patienten (für Filter).
    /// </summary>
    private readonly IPatientService _patientService;

    /// <summary>
    /// Konstruktor des Fensters.
    /// Initialisiert die Services und lädt beim Öffnen die Daten.
    /// </summary>
    public AppointmentCalendarWindow(
        IAppointmentService appointmentService,
        IPatientService patientService)
    {
        InitializeComponent();
        _appointmentService = appointmentService;
        _patientService = patientService;

        Loaded += AppointmentCalendarWindow_Loaded;
    }

    /// <summary>
    /// Wird beim Laden des Fensters ausgeführt.
    /// 
    /// - setzt den Wochenstart (Montag)
    /// - lädt alle Patienten für den Filter
    /// - lädt die Termine
    /// </summary>
    private async void AppointmentCalendarWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Startdatum auf Montag der aktuellen Woche setzen
        WeekDatePicker.SelectedDate = GetStartOfWeek(DateTime.Today);

        // Patienten für Filter laden
        var patients = await _patientService.GetAllPatientsAsync();
        PatientFilterComboBox.ItemsSource = patients.ToList();

        // Termine laden
        await LoadAppointmentsAsync();
    }

    /// <summary>
    /// Berechnet den Montag der Woche eines gegebenen Datums.
    /// </summary>
    /// <param name="date">Ein beliebiges Datum.</param>
    /// <returns>Der Montag dieser Woche.</returns>
    private DateTime GetStartOfWeek(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    /// <summary>
    /// Lädt alle Termine für die ausgewählte Woche
    /// und optional für einen bestimmten Patienten.
    /// </summary>
    private async Task LoadAppointmentsAsync()
    {
        if (WeekDatePicker.SelectedDate == null)
            return;

        // Optionaler Patient-Filter
        int? patientId = PatientFilterComboBox.SelectedValue is int id ? id : null;

        var appointments = await _appointmentService
            .GetAppointmentsByWeekAndPatientAsync(WeekDatePicker.SelectedDate.Value, patientId);

        // Daten im Grid anzeigen
        CalendarDataGrid.ItemsSource = appointments;
    }

    /// <summary>
    /// Wird ausgelöst, wenn das Datum geändert wird.
    /// 
    /// Setzt automatisch den Wochenstart (Montag)
    /// und lädt die Termine neu.
    /// </summary>
    private async void WeekDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WeekDatePicker.SelectedDate.HasValue)
        {
            WeekDatePicker.SelectedDate = GetStartOfWeek(WeekDatePicker.SelectedDate.Value);
            await LoadAppointmentsAsync();
        }
    }

    /// <summary>
    /// Wird ausgelöst, wenn ein Patient im Filter ausgewählt wird.
    /// Lädt die Termine neu.
    /// </summary>
    private async void PatientFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadAppointmentsAsync();
    }

    /// <summary>
    /// Zeigt alle Termine unabhängig vom Patienten an.
    /// </summary>
    private async void ShowAllPatients_Click(object sender, RoutedEventArgs e)
    {
        PatientFilterComboBox.SelectedItem = null;
        await LoadAppointmentsAsync();
    }

    /// <summary>
    /// Aktualisiert die Terminliste manuell.
    /// </summary>
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadAppointmentsAsync();
    }

    /// <summary>
    /// Öffnet das Bearbeitungsfenster per Doppelklick auf einen Termin.
    /// </summary>
    private async void CalendarDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (CalendarDataGrid.SelectedItem is not Appointment selectedAppointment)
            return;

        await OpenEditWindowAsync(selectedAppointment.Id);
    }

    /// <summary>
    /// Öffnet das Bearbeitungsfenster über einen Button.
    /// </summary>
    private async void EditAppointment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Appointment appointment)
        {
            await OpenEditWindowAsync(appointment.Id);
        }
    }

    /// <summary>
    /// Öffnet das Fenster zum Bearbeiten eines Termins.
    /// 
    /// - lädt den Termin aus der Datenbank
    /// - öffnet das Bearbeitungsfenster
    /// - lädt danach die Termine neu
    /// </summary>
    private async Task OpenEditWindowAsync(int appointmentId)
    {
        var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);

        if (appointment == null)
        {
            MessageBox.Show("Termin wurde nicht gefunden.");
            return;
        }

        // Fenster über Dependency Injection erstellen
        var editWindow = App.ServiceProvider.GetRequiredService<AddAppointmentWindow>();
        editWindow.Owner = this;

        // Termin an das Fenster übergeben
        editWindow.SetAppointmentForEdit(appointment);

        var result = editWindow.ShowDialog();

        // Nach dem Speichern Liste aktualisieren
        if (result == true)
        {
            await LoadAppointmentsAsync();
        }
    }

    /// <summary>
    /// Löscht einen Termin nach Bestätigung durch den Benutzer.
    /// </summary>
    private async void DeleteAppointment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not Appointment appointment)
            return;

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
            await _appointmentService.DeleteAppointmentAsync(appointment.Id);
            MessageBox.Show("Termin wurde gelöscht.");

            // Liste aktualisieren
            await LoadAppointmentsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}