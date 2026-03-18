using System.Windows;
using System.Windows.Controls;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using Praxis.Infrastructure.Services.Interface;

namespace Praxis.Client.Views;

public partial class OnlineBookingWindow : Window
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPatientService _patientService;

    public OnlineBookingWindow(
        IAppointmentService appointmentService,
        IPatientService patientService)
    {
        InitializeComponent();
        _appointmentService = appointmentService;
        _patientService = patientService;

        Loaded += OnlineBookingWindow_Loaded;
    }

    private async void OnlineBookingWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadPatientsAsync();
        AppointmentDatePicker.SelectedDate = DateTime.Today;
        await LoadAvailableSlotsAsync();
    }

    private async Task LoadPatientsAsync()
    {
        var patients = await _patientService.GetAllPatientsAsync();
        PatientComboBox.ItemsSource = patients;
    }
    private async Task LoadAvailableSlotsAsync()
    {
        SlotsListBox.ItemsSource = null;

        if (AppointmentDatePicker.SelectedDate == null)
            return;

        var selectedDate = AppointmentDatePicker.SelectedDate.Value;

        if (selectedDate.Date < DateTime.Today)
            return;

        if (selectedDate.DayOfWeek == DayOfWeek.Saturday || selectedDate.DayOfWeek == DayOfWeek.Sunday)
            return;

        var duration = GetSelectedDuration();
        var slots = await _appointmentService.GetAvailableSlotsAsync(selectedDate, duration);

        SlotsListBox.ItemsSource = slots;
    }
    private int GetSelectedDuration()
    {
        if (DurationComboBox.SelectedItem is ComboBoxItem item &&
            int.TryParse(item.Content?.ToString(), out var duration))
        {
            return duration;
        }

        return 15;
    }
    private async void AppointmentDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadAvailableSlotsAsync();
    }
    private async void DurationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
        {
            await LoadAvailableSlotsAsync();
        }
    }
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
    private async void BookButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (PatientComboBox.SelectedValue is not int patientId)
            {
                MessageBox.Show("Bitte einen Patienten auswählen.");
                return;
            }

            if (SlotsListBox.SelectedItem is not DateTime selectedStartTime)
            {
                MessageBox.Show("Bitte einen freien Termin auswählen.");
                return;
            }

            var duration = GetSelectedDuration();

            if (selectedStartTime.Date < DateTime.Today)
            {
                MessageBox.Show("Termine in der Vergangenheit können nicht gebucht werden.");
                return;
            }

            if (selectedStartTime < DateTime.Now)
            {
                MessageBox.Show("Vergangene Uhrzeiten können nicht gebucht werden.");
                return;
            }

            var isAvailable = await _appointmentService.IsTimeSlotAvailableAsync(selectedStartTime, duration);
            if (!isAvailable)
            {
                MessageBox.Show("Der Termin wurde inzwischen vergeben. Bitte wähle einen anderen Slot.");
                await LoadAvailableSlotsAsync();
                return;
            }

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

            var appointment = new Appointment
            {
                PatientId = patientId,
                StartTime = selectedStartTime,
                DurationMinutes = duration,
                Reason = "Online gebucht",
                Status = "Bestätigt"
            };

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