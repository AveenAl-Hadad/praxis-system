using System.Globalization;
using System.Windows;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using Praxis.Infrastructure.Services.Interface;

namespace Praxis.Client.Views;

public partial class AddAppointmentWindow : Window
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPatientService _patientService;
    private Appointment? _editingAppointment;

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
        StatusComboBox.ItemsSource = new List<string>
        {
            "Geplant",
            "Bestätigt",
            "Abgesagt",
            "Erledigt"
        };
        // wenn Termin bearbeitet wird
        if (_editingAppointment != null)
        {
            PatientComboBox.SelectedValue = _editingAppointment.PatientId;
            AppointmentDatePicker.SelectedDate = _editingAppointment.StartTime.Date;
            TimeTextBox.Text = _editingAppointment.StartTime.ToString("HH:mm");
            DurationTextBox.Text = _editingAppointment.DurationMinutes.ToString();
            ReasonTextBox.Text = _editingAppointment.Reason;
            // Status setzen
            StatusComboBox.SelectedItem = _editingAppointment.Status;
        }

        else
        {
            AppointmentDatePicker.SelectedDate = DateTime.Today;
            StatusComboBox.SelectedItem = "Geplant";
        }
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

            if (!TimeSpan.TryParseExact(TimeTextBox.Text, @"hh\:mm", null, out var time))
            {
                MessageBox.Show("Bitte Uhrzeit im Format HH:mm eingeben.");
                return;
            }

            if (!int.TryParse(DurationTextBox.Text, out var duration) || duration <= 0)
            {
                MessageBox.Show("Bitte gültige Dauer eingeben.");
                return;
            }

            if (string.IsNullOrWhiteSpace(ReasonTextBox.Text))
            {
                MessageBox.Show("Bitte Grund eingeben.");
                return;
            }

            var startTime = AppointmentDatePicker.SelectedDate.Value.Date.Add(time);
            var selectedStatus = StatusComboBox.SelectedItem?.ToString() ?? "Geplant";

            if (_editingAppointment == null)
            {
                var newAppointment = new Appointment
                {
                    PatientId = selectedPatient.Id,
                    StartTime = startTime,
                    DurationMinutes = duration,
                    Reason = ReasonTextBox.Text.Trim(),
                    Status = selectedStatus
                };

                await _appointmentService.AddAppointmentAsync(newAppointment);
                MessageBox.Show("Termin wurde gespeichert.");
            }
            else
            {
                _editingAppointment.PatientId = selectedPatient.Id;
                _editingAppointment.StartTime = startTime;
                _editingAppointment.DurationMinutes = duration;
                _editingAppointment.Reason = ReasonTextBox.Text.Trim();
                _editingAppointment.Status = selectedStatus;

                await _appointmentService.UpdateAppointmentAsync(_editingAppointment);
                MessageBox.Show("Termin wurde aktualisiert.");
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
    public void SetAppointmentForEdit(Appointment appointment)
    {
        _editingAppointment = appointment;
        Title = "Termin bearbeiten";
    }
}