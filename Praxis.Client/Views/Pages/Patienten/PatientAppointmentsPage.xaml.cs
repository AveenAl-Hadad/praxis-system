using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views.Pages.Patienten
{
    public partial class PatientAppointmentsPage : System.Windows.Controls.UserControl
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IRoomService _roomService;

        private Patient? _currentPatient;
        private Appointment? _selectedAppointment;
        private bool _isLoadingForm;
        private ListBox? _availableSlotsListBox;

        public PatientAppointmentsPage(
            IAppointmentService appointmentService,
            IRoomService roomService)
        {
            InitializeComponent();

            _appointmentService = appointmentService;
            _roomService = roomService;

            AppointmentDatePicker.SelectedDate = DateTime.Today;
            Loaded += PatientAppointmentsPage_Loaded;

        }
        private async void PatientAppointmentsPage_Loaded(object sender, RoutedEventArgs e)
        {
            _availableSlotsListBox = FindName("AvailableSlotsListBox") as ListBox;

            if (AppointmentDatePicker != null)
                AppointmentDatePicker.SelectedDate = DateTime.Today;

            await RefreshAvailableSlotsAsync();
        }

        public async Task LoadPatientAsync(Patient patient)
        {
            _currentPatient = patient;

            PatientNameTextBox.Text = patient.FullName;
            GeburtsdatumTextBox.Text = patient.Geburtsdatum.ToString("dd.MM.yyyy");
            TelefonTextBox.Text = patient.Telefonnummer;
            EmailTextBox.Text = patient.Email;

            await LoadRoomsAsync();
            await RefreshAppointmentsAsync();
            ClearForm();
            await RefreshAvailableSlotsAsync();
        }

        private async Task LoadRoomsAsync()
        {
            var rooms = await _roomService.GetActiveAsync();
            RoomComboBox.ItemsSource = rooms;

            if (rooms.Count > 0)
                RoomComboBox.SelectedIndex = 0;
        }

        private async Task RefreshAppointmentsAsync()
        {
            if (_currentPatient == null)
                return;

            var appointments = await _appointmentService.GetAllAppointmentsAsync();

            AppointmentsGrid.ItemsSource = appointments
                .Where(a => a.PatientId == _currentPatient.Id)
                .OrderBy(a => a.StartTime)
                .ToList();
        }

        private void ClearForm()
        {
            _isLoadingForm = true;

            _selectedAppointment = null;

            if (AppointmentDatePicker != null)
                AppointmentDatePicker.SelectedDate = DateTime.Today;

            if (AppointmentTimeTextBox != null)
                AppointmentTimeTextBox.Text = "09:00";

            if (DurationTextBox != null)
                DurationTextBox.Text = "30";

            if (ReasonTextBox != null)
                ReasonTextBox.Text = string.Empty;

            if (StatusComboBox != null && StatusComboBox.Items.Count > 0)
                StatusComboBox.SelectedIndex = 0;

            if (RoomComboBox != null && RoomComboBox.Items.Count > 0)
                RoomComboBox.SelectedIndex = 0;

            if (AppointmentsGrid != null)
                AppointmentsGrid.SelectedItem = null;

            // 🔥 WICHTIG: sichere Variante
            var listBox = AvailableSlotsListBox ?? FindName("AvailableSlotsListBox") as ListBox;

            if (listBox != null)
                listBox.SelectedItem = null;

            _isLoadingForm = false;
        }

        private async Task RefreshAvailableSlotsAsync()
        {
            if (_isLoadingForm)
                return;

            _availableSlotsListBox ??= FindName("AvailableSlotsListBox") as ListBox;

            if (_availableSlotsListBox == null)
                return;

            if (AppointmentDatePicker == null || DurationTextBox == null || RoomComboBox == null)
                return;

            _availableSlotsListBox.ItemsSource = null;

            if (AppointmentDatePicker.SelectedDate == null)
                return;

            if (!int.TryParse(DurationTextBox.Text, out var duration) || duration <= 0)
                return;

            var roomName = RoomComboBox.SelectedValue?.ToString();
            if (string.IsNullOrWhiteSpace(roomName))
                return;

            List<DateTime> slots;

            if (_selectedAppointment == null)
            {
                slots = await _appointmentService.GetAvailableSlotsAsync(
                    AppointmentDatePicker.SelectedDate.Value,
                    duration,
                    roomName);
            }
            else
            {
                slots = await _appointmentService.GetAvailableSlotsForEditAsync(
                    AppointmentDatePicker.SelectedDate.Value,
                    duration,
                    roomName,
                    _selectedAppointment.Id);
            }

            var items = slots
                .Select(s => new AvailableSlotItem
                {
                    SlotTime = s,
                    IsCurrentAppointmentSlot = _selectedAppointment != null &&
                                               s == _selectedAppointment.StartTime &&
                                               _selectedAppointment.StartTime.Date == AppointmentDatePicker.SelectedDate.Value.Date &&
                                               string.Equals(_selectedAppointment.RoomName, roomName, StringComparison.OrdinalIgnoreCase),
                    SlotLabel = BuildSlotLabel(s, roomName)
                })
                .OrderBy(x => x.SlotTime)
                .ToList();

            _availableSlotsListBox.ItemsSource = items;

            if (_selectedAppointment != null)
            {
                var selectedItem = items.FirstOrDefault(x => x.SlotTime == _selectedAppointment.StartTime);
                if (selectedItem != null)
                    _availableSlotsListBox.SelectedItem = selectedItem;
            }
        }
        private string BuildSlotLabel(DateTime slotTime, string roomName)
        {
            var isCurrent = _selectedAppointment != null &&
                            slotTime == _selectedAppointment.StartTime &&
                            _selectedAppointment.StartTime.Date == slotTime.Date &&
                            string.Equals(_selectedAppointment.RoomName, roomName, StringComparison.OrdinalIgnoreCase);

            if (isCurrent)
            {
                return $"{slotTime:HH:mm} Uhr  |  {roomName}  |  aktueller Termin";
            }

            return $"{slotTime:HH:mm} Uhr  |  {roomName}";
        }

        private async void SaveAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPatient == null)
            {
                MessageBox.Show("Kein Patient geladen.");
                return;
            }

            try
            {
                var startTime = BuildStartTime();
                var duration = ParseDuration();
                var reason = ReasonTextBox.Text?.Trim() ?? string.Empty;
                var roomName = RoomComboBox.SelectedValue?.ToString() ?? string.Empty;
                var status = GetSelectedStatus();

                if (string.IsNullOrWhiteSpace(roomName))
                    throw new InvalidOperationException("Bitte einen Raum auswählen.");

                if (_selectedAppointment == null)
                {
                    var appointment = new Appointment
                    {
                        PatientId = _currentPatient.Id,
                        StartTime = startTime,
                        DurationMinutes = duration,
                        Reason = reason,
                        Status = status,
                        RoomName = roomName,
                        TreatmentState = "Geplant"
                    };

                    await _appointmentService.AddAppointmentAsync(appointment);

                    MessageBox.Show("Termin wurde angelegt.", "Erfolg",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _selectedAppointment.StartTime = startTime;
                    _selectedAppointment.DurationMinutes = duration;
                    _selectedAppointment.Reason = reason;
                    _selectedAppointment.Status = status;
                    _selectedAppointment.RoomName = roomName;

                    await _appointmentService.UpdateAppointmentAsync(_selectedAppointment);

                    MessageBox.Show("Termin wurde aktualisiert.", "Erfolg",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                await RefreshAppointmentsAsync();
                ClearForm();
                await RefreshAvailableSlotsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAppointment == null)
            {
                MessageBox.Show("Bitte zuerst einen Termin auswählen.");
                return;
            }

            var result = MessageBox.Show(
                $"Termin am {_selectedAppointment.StartTime:dd.MM.yyyy HH:mm} wirklich löschen?",
                "Bestätigung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await _appointmentService.DeleteAppointmentAsync(_selectedAppointment.Id);
                await RefreshAppointmentsAsync();
                ClearForm();
                await RefreshAvailableSlotsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void NewAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            await RefreshAvailableSlotsAsync();
        }

        private async void RefreshSlotsButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshAvailableSlotsAsync();
        }

        private async void AppointmentCriteria_Changed(object sender, RoutedEventArgs e)
        {
            await RefreshAvailableSlotsAsync();
        }

        private void AvailableSlotsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AvailableSlotsListBox.SelectedItem is not AvailableSlotItem item)
                return;

            AppointmentTimeTextBox.Text = item.SlotTime.ToString("HH:mm");
        }

        private async void AppointmentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppointmentsGrid.SelectedItem is not Appointment appointment)
                return;

            _isLoadingForm = true;

            _selectedAppointment = appointment;

            AppointmentDatePicker.SelectedDate = appointment.StartTime.Date;
            AppointmentTimeTextBox.Text = appointment.StartTime.ToString("HH:mm");
            DurationTextBox.Text = appointment.DurationMinutes.ToString();
            ReasonTextBox.Text = appointment.Reason;

            SelectStatus(appointment.Status);
            RoomComboBox.SelectedValue = appointment.RoomName;

            _isLoadingForm = false;

            await RefreshAvailableSlotsAsync();
        }

        private DateTime BuildStartTime()
        {
            if (AppointmentDatePicker.SelectedDate == null)
                throw new InvalidOperationException("Bitte ein Datum auswählen.");

            if (!TimeSpan.TryParse(AppointmentTimeTextBox.Text, out var time))
                throw new InvalidOperationException("Uhrzeit ist ungültig. Format: HH:mm");

            var date = AppointmentDatePicker.SelectedDate.Value.Date;
            return date.Add(time);
        }

        private int ParseDuration()
        {
            if (!int.TryParse(DurationTextBox.Text, out var duration) || duration <= 0)
                throw new InvalidOperationException("Bitte eine gültige Dauer in Minuten eingeben.");

            return duration;
        }

        private string GetSelectedStatus()
        {
            if (StatusComboBox.SelectedItem is ComboBoxItem item &&
                item.Content is string text &&
                !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            return "Geplant";
        }

        private void SelectStatus(string status)
        {
            foreach (var item in StatusComboBox.Items.OfType<ComboBoxItem>())
            {
                if (string.Equals(item.Content?.ToString(), status, StringComparison.OrdinalIgnoreCase))
                {
                    StatusComboBox.SelectedItem = item;
                    return;
                }
            }

            StatusComboBox.SelectedIndex = 0;
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                await mainWindow.OpenPatientSearchPageAsync();
            }
        }

        private sealed class AvailableSlotItem
        {
            public DateTime SlotTime { get; set; }
            public string SlotLabel { get; set; } = string.Empty;
            public bool IsCurrentAppointmentSlot { get; set; }
        }
    }
}