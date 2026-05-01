using Praxis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views.Pages.Patienten.PatientAppointment
{
    public partial class PatientAppointmentsPage
    {
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
                await RefreshRoomPlannerAsync();
                await RefreshPatientFlowAsync();
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
                await RefreshRoomPlannerAsync();
                await RefreshPatientFlowAsync();
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
            await RefreshRoomPlannerAsync();
        }
        private async void RefreshSlotsButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshAvailableSlotsAsync();
        }
        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                await mainWindow.OpenPatientSearchPageAsync();
            }
        }

        //Check-in
        private async void CheckInMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var appointmentId = GetAppointmentIdFromMenuSender(sender);
            if (appointmentId == null)
                return;

            try
            {
                await _appointmentService.CheckInAsync(appointmentId.Value);

                await RefreshAppointmentsAsync();
                await RefreshAvailableSlotsAsync();
                await RefreshRoomPlannerAsync();
                await OpenAppointmentInFormAsync(appointmentId.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //In Behandlung
        private async void InTreatmentMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var appointmentId = GetAppointmentIdFromMenuSender(sender);
            if (appointmentId == null)
                return;

            try
            {
                var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId.Value);
                if (appointment == null)
                    return;

                appointment.TreatmentState = "In Behandlung";

                await _appointmentService.UpdateAppointmentAsync(appointment);

                await RefreshAppointmentsAsync();
                await RefreshAvailableSlotsAsync();
                await RefreshRoomPlannerAsync();
                await OpenAppointmentInFormAsync(appointmentId.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //Abschließen
        private async void CompleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var appointmentId = GetAppointmentIdFromMenuSender(sender);
            if (appointmentId == null)
                return;

            try
            {
                await _appointmentService.CompleteAppointmentAsync(appointmentId.Value);

                await RefreshAppointmentsAsync();
                await RefreshAvailableSlotsAsync();
                await RefreshRoomPlannerAsync();
                await OpenAppointmentInFormAsync(appointmentId.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //Absagen
        private async void CancelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var appointmentId = GetAppointmentIdFromMenuSender(sender);
            if (appointmentId == null)
                return;

            var result = MessageBox.Show(
                "Termin wirklich absagen?",
                "Bestätigung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await _appointmentService.CancelAppointmentAsync(appointmentId.Value, "Abgesagt im Kalender");

                await RefreshAppointmentsAsync();
                await RefreshAvailableSlotsAsync();
                await RefreshRoomPlannerAsync();
                await OpenAppointmentInFormAsync(appointmentId.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //Raum wechseln
        private async void MoveRoomMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var appointmentId = GetAppointmentIdFromMenuSender(sender);
            if (appointmentId == null)
                return;

            try
            {
                var rooms = await _roomService.GetActiveAsync();
                if (rooms.Count == 0)
                {
                    MessageBox.Show("Es sind keine aktiven Räume vorhanden.");
                    return;
                }

                var dialog = new SelectRoomWindow(rooms.Select(r => r.Name).ToList());
                dialog.Owner = Window.GetWindow(this);

                var dialogResult = dialog.ShowDialog();
                if (dialogResult != true || string.IsNullOrWhiteSpace(dialog.SelectedRoomName))
                    return;

                await _appointmentService.MoveToRoomAsync(appointmentId.Value, dialog.SelectedRoomName);

                await RefreshAppointmentsAsync();
                await RefreshAvailableSlotsAsync();
                await RefreshRoomPlannerAsync();
                await OpenAppointmentInFormAsync(appointmentId.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //Buttons für Statuswechsel  Jetzt kommen die Aktionen.
        // 1) Öffnen
        private async void FlowOpenButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Button geklickt");
            if (sender is not Button button || button.Tag is not int appointmentId)
                return;

            await OpenAppointmentInFormAsync(appointmentId);
        }
        // 2) Nach Wartet
        private async void MoveToWaitingButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not int appointmentId)
                return;

            await UpdateFlowStateAsync(appointmentId, "Wartet");
        }
        // 3) Nach In Behandlung
        private async void MoveToInTreatmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not int appointmentId)
                return;

            await UpdateFlowStateAsync(appointmentId, "In Behandlung");
        }
        // 4) Nach Abgeschlossen
        private async void MoveToCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not int appointmentId)
                return;

            await CompleteFlowAppointmentAsync(appointmentId);
        }



       

    }
}
