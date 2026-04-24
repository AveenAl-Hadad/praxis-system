using System.Windows;
using System.Windows.Controls;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;

using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views;

public partial class OnlineBookingWindow : Window
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPatientService _patientService;
    private readonly IDoctorService _doctorService;
    private readonly IAppointmentTypeService _appointmentTypeService;

    public OnlineBookingWindow(
        IAppointmentService appointmentService,
        IPatientService patientService,
        IDoctorService doctorService,
        IAppointmentTypeService appointmentTypeService)
    {
        InitializeComponent();
        _appointmentService = appointmentService;
        _patientService = patientService;
        _doctorService = doctorService;
        _appointmentTypeService = appointmentTypeService;

        Loaded += OnlineBookingWindow_Loaded;
    }

    private async void OnlineBookingWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadPatientsAsync();
        await LoadAppointmentTypesAsync();
        await LoadDoctorsAsync();

        AppointmentDatePicker.SelectedDate = DateTime.Today;

        UpdateBookingRulesText();
        await LoadAvailableSlotsAsync();
    }

    private async Task LoadPatientsAsync()
    {
        PatientComboBox.ItemsSource = await _patientService.GetAllPatientsAsync();
    }

    private async Task LoadAppointmentTypesAsync()
    {
        var items = await _appointmentTypeService.GetOnlineBookableAsync();
        AppointmentTypeComboBox.ItemsSource = items;

        if (items.Count > 0)
            AppointmentTypeComboBox.SelectedIndex = 0;
    }

    private async Task LoadDoctorsAsync()
    {
        var doctors = (await _doctorService.GetActiveAsync())
            .Where(d => d.AllowOnlineBooking)
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .ToList();

        DoctorComboBox.ItemsSource = doctors;

        if (doctors.Count > 0)
        {
            DoctorComboBox.SelectedIndex = 0;
        }
        else
        {
            DoctorComboBox.SelectedIndex = -1;
            BookingRulesTextBlock.Text += "\n\nAktuell ist kein online buchbarer Behandler vorhanden.";
        }
    }

    private void UpdateBookingRulesText()
    {
        if (AppointmentTypeComboBox.SelectedItem is not AppointmentType type)
        {
            BookingRulesTextBlock.Text = "Bitte zuerst eine Terminart auswählen.";
            return;
        }

        BookingRulesTextBlock.Text =
            $"Dauer: {type.DurationMinutes} Minuten\n" +
            $"Früheste Online-Buchung: {type.MinLeadHours} Stunden im Voraus\n" +
            $"Maximale Vorausbuchung: {type.MaxAdvanceDays} Tage";
    }

    private async Task LoadAvailableSlotsAsync()
    {
        SlotsListBox.ItemsSource = null;

        if (AppointmentDatePicker.SelectedDate == null)
        {
            BookingRulesTextBlock.Text = "Bitte ein Datum auswählen.";
            return;
        }

        if (AppointmentTypeComboBox.SelectedItem is not AppointmentType selectedType)
        {
            BookingRulesTextBlock.Text = "Bitte eine Terminart auswählen.";
            return;
        }

        if (DoctorComboBox.SelectedItem is not Doctor selectedDoctor)
        {
            BookingRulesTextBlock.Text = "Bitte einen Behandler auswählen.";
            return;
        }

        var selectedDate = AppointmentDatePicker.SelectedDate.Value;

        if (selectedDate.Date < DateTime.Today)
        {
            BookingRulesTextBlock.Text =
                $"Dauer: {selectedType.DurationMinutes} Minuten\n" +
                $"Früheste Online-Buchung: {selectedType.MinLeadHours} Stunden im Voraus\n" +
                $"Maximale Vorausbuchung: {selectedType.MaxAdvanceDays} Tage\n\n" +
                $"Das gewählte Datum liegt in der Vergangenheit.";
            return;
        }

        var slots = await _appointmentService.GetAvailableOnlineSlotsAsync(
            selectedDate,
            selectedType.Id,
            selectedDoctor.Id);

        SlotsListBox.ItemsSource = slots;
        SlotsListBox.ItemsSource = slots;

        if (slots.Count > 0)
        {
            SlotsListBox.SelectedIndex = 0;
            BookButton.IsEnabled = true;

            BookingRulesTextBlock.Text =
                $"Dauer: {selectedType.DurationMinutes} Minuten\n" +
                $"Früheste Online-Buchung: {selectedType.MinLeadHours} Stunden im Voraus\n" +
                $"Maximale Vorausbuchung: {selectedType.MaxAdvanceDays} Tage\n\n" +
                $"Freie Termine gefunden: {slots.Count}";
        }
        else
        {
            SlotsListBox.SelectedIndex = -1;
            BookButton.IsEnabled = false;

            BookingRulesTextBlock.Text =
                $"Dauer: {selectedType.DurationMinutes} Minuten\n" +
                $"Früheste Online-Buchung: {selectedType.MinLeadHours} Stunden im Voraus\n" +
                $"Maximale Vorausbuchung: {selectedType.MaxAdvanceDays} Tage\n\n" +
                $"Für {selectedDoctor.FullName} wurden an diesem Datum keine freien Online-Termine gefunden.";
        }
    }

    private async void AppointmentDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            await LoadAvailableSlotsAsync();
    }

    private async void AppointmentTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        UpdateBookingRulesText();
        await LoadDoctorsAsync();
        await LoadAvailableSlotsAsync();
    }

    private async void DoctorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            await LoadAvailableSlotsAsync();
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

            if (AppointmentTypeComboBox.SelectedValue is not int appointmentTypeId)
            {
                MessageBox.Show("Bitte eine Terminart auswählen.");
                return;
            }

            if (DoctorComboBox.SelectedValue is not int doctorId)
            {
                MessageBox.Show("Bitte einen Behandler auswählen.");
                return;
            }

            if (SlotsListBox.SelectedItem is not DateTime selectedStartTime)
            {
                MessageBox.Show(
                    "Es wurde keine Uhrzeit ausgewählt. Bitte zuerst einen freien Termin in der Liste auswählen.",
                    "Zeit auswählen",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            await _appointmentService.AddOnlineAppointmentAsync(
                patientId,
                appointmentTypeId,
                doctorId,
                selectedStartTime);

            MessageBox.Show("Termin wurde erfolgreich online gebucht.");
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SlotsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        BookButton.IsEnabled = SlotsListBox.SelectedItem is DateTime;
    }
}