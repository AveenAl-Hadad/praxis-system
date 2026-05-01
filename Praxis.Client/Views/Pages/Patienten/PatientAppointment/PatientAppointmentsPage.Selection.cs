using Praxis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using System.Windows.Input;



namespace Praxis.Client.Views.Pages.Patienten.PatientAppointment
{
    public partial class PatientAppointmentsPage
    {
        private async void AppointmentCriteria_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (AppointmentDatePicker.SelectedDate.HasValue)
            {
                _plannerSelectedDate = AppointmentDatePicker.SelectedDate.Value.Date;
            }

            await RefreshAvailableSlotsAsync();
            await RefreshRoomPlannerAsync();
            await RefreshPatientFlowAsync();
        }
        private async void AppointmentDurationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await RefreshAvailableSlotsAsync();
            await RefreshRoomPlannerAsync();
            await RefreshPatientFlowAsync();
        }
        private async void RoomComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await RefreshAvailableSlotsAsync();
            await RefreshRoomPlannerAsync();
            await RefreshPatientFlowAsync();
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
            await LoadAppointmentMedicalEntriesAsync(appointment.Id);
        }
        
        // Autocomplete für Diagnose
        private async void AppointmentDiagnosisSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var search = AppointmentDiagnosisSearchBox.Text?.Trim() ?? "";

            if (search.Length < 2)
            {
                AppointmentDiagnosisSuggestionList.Visibility = Visibility.Collapsed;
                AppointmentDiagnosisSuggestionList.ItemsSource = null;
                return;
            }

            var result = await _appointmentMedicalEntryService.SearchDiagnosisAsync(search);

            AppointmentDiagnosisSuggestionList.ItemsSource = result
                .Select(x => new AppointmentCatalogSuggestion { Item = x })
                .ToList();

            AppointmentDiagnosisSuggestionList.Visibility =
                AppointmentDiagnosisSuggestionList.Items.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void AppointmentDiagnosisSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (AppointmentDiagnosisSuggestionList.Visibility != Visibility.Visible)
                return;

            if (e.Key == Key.Down)
            {
                AppointmentDiagnosisSuggestionList.Focus();

                if (AppointmentDiagnosisSuggestionList.Items.Count > 0)
                    AppointmentDiagnosisSuggestionList.SelectedIndex = 0;

                e.Handled = true;
            }

            if (e.Key == Key.Enter)
            {
                if (AppointmentDiagnosisSuggestionList.SelectedIndex < 0 &&
                    AppointmentDiagnosisSuggestionList.Items.Count > 0)
                {
                    AppointmentDiagnosisSuggestionList.SelectedIndex = 0;
                }

                SelectAppointmentDiagnosisSuggestion();
                e.Handled = true;
            }
        }

        private void AppointmentDiagnosisSuggestionList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SelectAppointmentDiagnosisSuggestion();
                e.Handled = true;
            }
        }

        private void AppointmentDiagnosisSuggestionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectAppointmentDiagnosisSuggestion();
        }

        private void SelectAppointmentDiagnosisSuggestion()
        {
            if (AppointmentDiagnosisSuggestionList.SelectedItem is not AppointmentCatalogSuggestion suggestion)
                return;

            _selectedAppointmentDiagnosis = suggestion.Item;
            AppointmentDiagnosisSearchBox.Text = $"{suggestion.Item.Code} - {suggestion.Item.Name}";
            AppointmentDiagnosisSuggestionList.Visibility = Visibility.Collapsed;
        }

        //Autocomplete für Leistung
        private async void AppointmentServiceSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var search = AppointmentServiceSearchBox.Text?.Trim() ?? "";

            if (search.Length < 1)
            {
                AppointmentServiceSuggestionList.Visibility = Visibility.Collapsed;
                AppointmentServiceSuggestionList.ItemsSource = null;
                return;
            }

            var result = await _appointmentMedicalEntryService.SearchServiceAsync(search);

            AppointmentServiceSuggestionList.ItemsSource = result
                .Select(x => new AppointmentCatalogSuggestion { Item = x })
                .ToList();

            AppointmentServiceSuggestionList.Visibility =
                AppointmentServiceSuggestionList.Items.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }
        private void AppointmentServiceSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (AppointmentServiceSuggestionList.Visibility != Visibility.Visible)
                return;

            if (e.Key == Key.Down)
            {
                AppointmentServiceSuggestionList.Focus();

                if (AppointmentServiceSuggestionList.Items.Count > 0)
                    AppointmentServiceSuggestionList.SelectedIndex = 0;

                e.Handled = true;
            }

            if (e.Key == Key.Enter)
            {
                if (AppointmentServiceSuggestionList.SelectedIndex < 0 &&
                    AppointmentServiceSuggestionList.Items.Count > 0)
                {
                    AppointmentServiceSuggestionList.SelectedIndex = 0;
                }

                SelectAppointmentServiceSuggestion();
                e.Handled = true;
            }
        }
        private void AppointmentServiceSuggestionList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SelectAppointmentServiceSuggestion();
                e.Handled = true;
            }
        }
        private void AppointmentServiceSuggestionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectAppointmentServiceSuggestion();
        }
        private void SelectAppointmentServiceSuggestion()
        {
            if (AppointmentServiceSuggestionList.SelectedItem is not AppointmentCatalogSuggestion suggestion)
                return;

            _selectedAppointmentService = suggestion.Item;
            AppointmentServiceSearchBox.Text = $"{suggestion.Item.Code} - {suggestion.Item.Name}";
            AppointmentServiceSuggestionList.Visibility = Visibility.Collapsed;
        }

        //Hinzufügen/Löschen
        private async void AddAppointmentMedicalEntry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedAppointment == null)
                {
                    MessageBox.Show("Bitte zuerst einen Termin auswählen oder speichern.");
                    return;
                }

                await _appointmentMedicalEntryService.AddAsync(
                    _selectedAppointment.Id,
                    _selectedAppointmentDiagnosis?.Id,
                    _selectedAppointmentService?.Id,
                    AppointmentMedicalNotesBox.Text);

                _selectedAppointmentDiagnosis = null;
                _selectedAppointmentService = null;

                AppointmentDiagnosisSearchBox.Clear();
                AppointmentServiceSearchBox.Clear();
                AppointmentMedicalNotesBox.Clear();

                await LoadAppointmentMedicalEntriesAsync(_selectedAppointment.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Diagnose / Leistung Fehler");
            }
        }
        private async void DeleteAppointmentMedicalEntry_Click(object sender, RoutedEventArgs e)
        {
            if (AppointmentMedicalEntriesGrid.SelectedItem is not AppointmentMedicalEntryRow row)
            {
                MessageBox.Show("Bitte zuerst einen Eintrag auswählen.");
                return;
            }

            await _appointmentMedicalEntryService.DeleteAsync(row.Id);

            if (_selectedAppointment != null)
                await LoadAppointmentMedicalEntriesAsync(_selectedAppointment.Id);
        }
        private async Task LoadAppointmentMedicalEntriesAsync(int appointmentId)
        {
            var entries = await _appointmentMedicalEntryService.GetByAppointmentAsync(appointmentId);

            _appointmentMedicalEntries.Clear();

            foreach (var entry in entries)
            {
                _appointmentMedicalEntries.Add(new AppointmentMedicalEntryRow
                {
                    Id = entry.Id,
                    Notes = entry.Notes,
                    DiagnosisText = entry.DiagnosisCatalogItem == null
                        ? ""
                        : $"{entry.DiagnosisCatalogItem.Code} - {entry.DiagnosisCatalogItem.Name}",
                    ServiceText = entry.ServiceCatalogItem == null
                        ? ""
                        : $"{entry.ServiceCatalogItem.Code} - {entry.ServiceCatalogItem.Name}"
                });
            }

        }
        private async Task OpenAppointmentInFormAsync(int appointmentId)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
            if (appointment == null)
            {
                MessageBox.Show("Termin NICHT gefunden!");
                return;
            }

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

            // Neu:
            MainPageScrollViewer?.ScrollToTop();
            AppointmentDatePicker?.Focus();
            await LoadAppointmentMedicalEntriesAsync(appointmentId);
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
    }
}
