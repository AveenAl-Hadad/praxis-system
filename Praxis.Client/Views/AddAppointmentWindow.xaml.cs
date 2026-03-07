using System.Globalization;
using System.Windows;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client;

public partial class AddAppointmentWindow : Window
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPatientService _patientService;

    public AddAppointmentWindow(
        IAppointmentService appointmentService,
        IPatientService patientService)
    {
        InitializeComponent();
        _appointmentService = appointmentService;
        _patientService = patientService;

        Loaded += AddAppointmentWindow_Loaded;
    }

    private async void AddAppointmentWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var patients = await _patientService.GetAllPatientsAsync();
        PatientComboBox.ItemsSource = patients.ToList();
        AppointmentDatePicker.SelectedDate = DateTime.Today;
    }

    private async void SaveAppointment_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (PatientComboBox.SelectedItem is not Patient selectedPatient)
            {
                MessageBox.Show("Bitte Patient auswählen.");
                return;
            }

            if (AppointmentDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Bitte Datum auswählen.");
                return;
            }

            if (!TimeSpan.TryParseExact(TimeTextBox.Text, @"hh\:mm", CultureInfo.InvariantCulture, out var time))
            {
                MessageBox.Show("Bitte Uhrzeit im Format HH:mm eingeben.");
                return;
            }

            if (!int.TryParse(DurationTextBox.Text, out int duration) || duration <= 0)
            {
                MessageBox.Show("Bitte gültige Dauer eingeben.");
                return;
            }

            if (string.IsNullOrWhiteSpace(ReasonTextBox.Text))
            {
                MessageBox.Show("Bitte Grund eingeben.");
                return;
            }
            if (PatientComboBox.SelectedItem == null)
            {
                MessageBox.Show("Bitte Patient auswählen.");
                return;
            }

            if (string.IsNullOrWhiteSpace(TimeTextBox.Text))
            {
                MessageBox.Show("Bitte Uhrzeit eingeben.");
                return;
            }

            var startTime = AppointmentDatePicker.SelectedDate.Value.Date.Add(time);

            var appointment = new Appointment
            {
                PatientId = selectedPatient.Id,
                StartTime = startTime,
                DurationMinutes = duration,
                Reason = ReasonTextBox.Text.Trim(),
                Status = "Geplant"
            };

            await _appointmentService.AddAppointmentAsync(appointment);

            MessageBox.Show("Termin wurde gespeichert.");
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Speichern: {ex.Message}");
        }
    }
}