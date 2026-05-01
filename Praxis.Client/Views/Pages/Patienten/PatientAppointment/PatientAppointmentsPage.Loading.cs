using Praxis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ListBox = System.Windows.Controls.ListBox; 

namespace Praxis.Client.Views.Pages.Patienten.PatientAppointment
{
    public partial class PatientAppointmentsPage
    {
        private async void PatientAppointmentsPage_Loaded(object sender, RoutedEventArgs e)
        {
            _availableSlotsListBox = FindName("AvailableSlotsListBox") as ListBox;

            if (AppointmentDatePicker != null)
                AppointmentDatePicker.SelectedDate = DateTime.Today;

            await RefreshAvailableSlotsAsync();
            await RefreshRoomPlannerAsync();
            await RefreshPatientFlowAsync();
        }
        public async Task LoadPatientAsync(Patient patient)
        {
            _currentPatient = patient;

            PatientNameTextBox.Text = patient.FullName;
            GeburtsdatumTextBox.Text = patient.Geburtsdatum.ToString("dd.MM.yyyy");
            TelefonTextBox.Text = patient.Telefonnummer;
            EmailTextBox.Text = patient.Email;

            _plannerSelectedDate = DateTime.Today;
            await LoadRoomsAsync();
            await InitializePlannerFiltersAsync();
            await RefreshAppointmentsAsync();
            ClearForm();
            await RefreshAvailableSlotsAsync();
            await RefreshRoomPlannerAsync();
            await RefreshPatientFlowAsync();
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
        private void ClearForm()
        {
            _isLoadingForm = true;

            _selectedAppointment = null;

            _appointmentMedicalEntries.Clear();
            _selectedAppointmentDiagnosis = null;
            _selectedAppointmentService = null;

            if (AppointmentDiagnosisSearchBox != null)
                AppointmentDiagnosisSearchBox.Clear();

            if (AppointmentServiceSearchBox != null)
                AppointmentServiceSearchBox.Clear();

            if (AppointmentMedicalNotesBox != null)
                AppointmentMedicalNotesBox.Clear();

            if (AppointmentDatePicker != null)
                AppointmentDatePicker.SelectedDate = _plannerSelectedDate;

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

            // sichere Variante
            var listBox = AvailableSlotsListBox ?? FindName("AvailableSlotsListBox") as ListBox;

            if (listBox != null)
                listBox.SelectedItem = null;

            _isLoadingForm = false;
        }
    }
}
