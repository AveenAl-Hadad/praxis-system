using Praxis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;

using DataObject = System.Windows.DataObject;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using ListBox = System.Windows.Controls.ListBox;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MessageBox = System.Windows.MessageBox;
using Brushes = System.Windows.Media.Brushes;


namespace Praxis.Client.Views.Pages.Patienten.PatientAppointment
{
    public partial class PatientAppointmentsPage
    {
        // Patientenfluss
        //Flow-Daten laden
        private async Task RefreshPatientFlowAsync()
        {
            if (AppointmentDatePicker.SelectedDate == null)
                return;

            var selectedDate = AppointmentDatePicker.SelectedDate.Value.Date;
            var appointments = await _appointmentService.GetAppointmentsByDateAsync(selectedDate);

            var filteredAppointments = appointments
                .Where(a => !string.Equals(a.Status, "Abgesagt", StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.StartTime)
                .ToList();

            var checkedInItems = filteredAppointments
                .Where(IsCheckedInFlowState)
                .Select(a => BuildFlowAppointmentItem(a, "CheckedIn"))
                .ToList();

            var waitingItems = filteredAppointments
                .Where(IsWaitingFlowState)
                .Select(a => BuildFlowAppointmentItem(a, "Waiting"))
                .ToList();

            var inTreatmentItems = filteredAppointments
                .Where(IsInTreatmentFlowState)
                .Select(a => BuildFlowAppointmentItem(a, "InTreatment"))
                .ToList();

            var completedItems = filteredAppointments
                .Where(IsCompletedFlowState)
                .Select(a => BuildFlowAppointmentItem(a, "Completed"))
                .ToList();

            CheckedInPatientsListBox.ItemsSource = checkedInItems;
            WaitingPatientsListBox.ItemsSource = waitingItems;
            InTreatmentPatientsListBox.ItemsSource = inTreatmentItems;
            CompletedPatientsListBox.ItemsSource = completedItems;
        }
        //Statuslogik für die vier Spalten
        private bool IsCheckedInFlowState(Appointment appointment)
        {
            // 🔥 auch geplante anzeigen
            if (!appointment.CheckInTime.HasValue &&
                string.IsNullOrWhiteSpace(appointment.TreatmentState))
            {
                return true;
            }

            return appointment.CheckInTime.HasValue &&
                   !string.Equals(appointment.TreatmentState, "Wartet", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(appointment.TreatmentState, "In Behandlung", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(appointment.TreatmentState, "Abgeschlossen", StringComparison.OrdinalIgnoreCase);
        }
        private bool IsWaitingFlowState(Appointment appointment)
        {
            return string.Equals(appointment.TreatmentState, "Wartet", StringComparison.OrdinalIgnoreCase);
        }
        private bool IsInTreatmentFlowState(Appointment appointment)
        {
            return string.Equals(appointment.TreatmentState, "In Behandlung", StringComparison.OrdinalIgnoreCase);
        }
        private bool IsCompletedFlowState(Appointment appointment)
        {
            return string.Equals(appointment.TreatmentState, "Abgeschlossen", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(appointment.Status, "Abgeschlossen", StringComparison.OrdinalIgnoreCase);
        }

        //Anzeige pro Eintrag bauen
        private FlowAppointmentItem BuildFlowAppointmentItem(Appointment appointment, string currentColumn)
        {
            var patientName = appointment.Patient?.FullName ?? $"Patient #{appointment.PatientId}";
            var room = string.IsNullOrWhiteSpace(appointment.RoomName) ? "Kein Raum" : appointment.RoomName;
            var reason = string.IsNullOrWhiteSpace(appointment.Reason) ? "Ohne Grund" : appointment.Reason.Trim();

            return new FlowAppointmentItem
            {
                AppointmentId = appointment.Id,
                Title = $"{appointment.StartTime:HH:mm} | {patientName}",
                Subtitle = $"{room} | {reason}",
                CurrentColumn = currentColumn,
                StatusIcon = GetFlowStatusIcon(currentColumn),
                WaitingTimeText = BuildWaitingTimeText(appointment, currentColumn)
            };
        }
        // Gemeinsame Statusänderung
        private async Task UpdateFlowStateAsync(int appointmentId, string treatmentState)
        {
            try
            {
                var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
                if (appointment == null)
                    return;

                appointment.TreatmentState = treatmentState;

                if (!appointment.CheckInTime.HasValue)
                {
                    appointment.CheckInTime = DateTime.Now;
                }

                await _appointmentService.UpdateAppointmentAsync(appointment);

                await RefreshAppointmentsAsync();
                await RefreshAvailableSlotsAsync();
                await RefreshRoomPlannerAsync();
                await RefreshPatientFlowAsync();
                await OpenAppointmentInFormAsync(appointmentId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Abschließen sauber behandeln
        private async Task CompleteFlowAppointmentAsync(int appointmentId)
        {
            try
            {
                await _appointmentService.CompleteAppointmentAsync(appointmentId);

                await RefreshAppointmentsAsync();
                await RefreshAvailableSlotsAsync();
                await RefreshRoomPlannerAsync();
                await RefreshPatientFlowAsync();
                await OpenAppointmentInFormAsync(appointmentId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //  Statuswechsel per Drop Jetzt kommt die eigentliche Logik.
        private async Task MoveFlowAppointmentByDropAsync(int appointmentId, string targetColumn)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
            if (appointment == null)
                return;

            switch (targetColumn)
            {
                case "CheckedIn":
                    if (!appointment.CheckInTime.HasValue)
                        appointment.CheckInTime = DateTime.Now;

                    appointment.TreatmentState = "Geplant";
                    break;

                case "Waiting":
                    if (!appointment.CheckInTime.HasValue)
                        appointment.CheckInTime = DateTime.Now;

                    appointment.TreatmentState = "Wartet";
                    break;

                case "InTreatment":
                    if (!appointment.CheckInTime.HasValue)
                        appointment.CheckInTime = DateTime.Now;

                    appointment.TreatmentState = "In Behandlung";
                    break;

                case "Completed":
                    await _appointmentService.CompleteAppointmentAsync(appointmentId);

                    await RefreshAppointmentsAsync();
                    await RefreshAvailableSlotsAsync();
                    await RefreshRoomPlannerAsync();
                    await RefreshPatientFlowAsync();
                    await OpenAppointmentInFormAsync(appointmentId);
                    return;

                default:
                    return;
            }

            await _appointmentService.UpdateAppointmentAsync(appointment);

            await RefreshAppointmentsAsync();
            await RefreshAvailableSlotsAsync();
            await RefreshRoomPlannerAsync();
            await RefreshPatientFlowAsync();
            await OpenAppointmentInFormAsync(appointmentId);
        }
        // Drag aus den Flow-Listen starten
        private void FlowListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not ListBox listBox)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _flowDragStartPoint = null;
                return;
            }

            var currentPosition = e.GetPosition(listBox);

            if (_flowDragStartPoint == null)
            {
                _flowDragStartPoint = currentPosition;
                return;
            }

            var diff = currentPosition - _flowDragStartPoint.Value;
            if (Math.Abs(diff.X) < 8 && Math.Abs(diff.Y) < 8)
                return;

            if (listBox.SelectedItem is not FlowAppointmentItem item)
                return;

            var sourceColumn = listBox.Tag?.ToString() ?? string.Empty;
            if (string.Equals(sourceColumn, "Completed", StringComparison.OrdinalIgnoreCase))
                return;

            var payload = new FlowDragPayload
            {
                AppointmentId = item.AppointmentId,
                SourceColumn = listBox.Tag?.ToString() ?? string.Empty
            };

            var data = new DataObject(typeof(FlowDragPayload), payload);
            DragDrop.DoDragDrop(listBox, data, DragDropEffects.Move);

            _flowDragStartPoint = null;
        }

        //Drop-Handler für die vier Spalten
        private void FlowListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(FlowDragPayload)))
            {
                e.Effects = DragDropEffects.None;
                ResetFlowDropHighlights();
                e.Handled = true;
                return;
            }

            if (sender is not ListBox targetListBox)
            {
                e.Effects = DragDropEffects.None;
                ResetFlowDropHighlights();
                e.Handled = true;
                return;
            }

            var payload = e.Data.GetData(typeof(FlowDragPayload)) as FlowDragPayload;
            var targetColumn = targetListBox.Tag?.ToString() ?? string.Empty;
            var isValid = payload != null && IsValidFlowDropTarget(payload.SourceColumn, targetColumn);

            HighlightFlowDropTarget(targetColumn, isValid);
            e.Effects = isValid ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }
        private void FlowListBox_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(FlowDragPayload)))
            {
                e.Effects = DragDropEffects.None;
                ResetFlowDropHighlights();
                e.Handled = true;
                return;
            }

            if (sender is not ListBox targetListBox)
            {
                e.Effects = DragDropEffects.None;
                ResetFlowDropHighlights();
                e.Handled = true;
                return;
            }

            var payload = e.Data.GetData(typeof(FlowDragPayload)) as FlowDragPayload;
            var targetColumn = targetListBox.Tag?.ToString() ?? string.Empty;
            var isValid = payload != null && IsValidFlowDropTarget(payload.SourceColumn, targetColumn);

            HighlightFlowDropTarget(targetColumn, isValid);
            e.Effects = isValid ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }
        private async void FlowListBox_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (!e.Data.GetDataPresent(typeof(FlowDragPayload)))
                    return;

                if (sender is not ListBox targetListBox)
                    return;

                var payload = e.Data.GetData(typeof(FlowDragPayload)) as FlowDragPayload;
                if (payload == null)
                    return;

                var targetColumn = targetListBox.Tag?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(targetColumn))
                    return;

                if (!IsValidFlowDropTarget(payload.SourceColumn, targetColumn))
                    return;

                await MoveFlowAppointmentByDropAsync(payload.AppointmentId, targetColumn);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _flowDragStartPoint = null;
                ResetFlowDropHighlights();
            }
        }
        private void FlowListBox_DragLeave(object sender, DragEventArgs e)
        {
            ResetFlowDropHighlights();
        }

        //Highlight-Hilfsmethoden einbauen Datei
        private void ResetFlowDropHighlights()
        {
            ApplyFlowBorderStyle(CheckedInFlowBorder, false, false);
            ApplyFlowBorderStyle(WaitingFlowBorder, false, false);
            ApplyFlowBorderStyle(InTreatmentFlowBorder, false, false);
            ApplyFlowBorderStyle(CompletedFlowBorder, false, false);
        }
        private void HighlightFlowDropTarget(string targetColumn, bool isValid)
        {
            ResetFlowDropHighlights();

            switch (targetColumn)
            {
                case "CheckedIn":
                    ApplyFlowBorderStyle(CheckedInFlowBorder, true, isValid);
                    break;

                case "Waiting":
                    ApplyFlowBorderStyle(WaitingFlowBorder, true, isValid);
                    break;

                case "InTreatment":
                    ApplyFlowBorderStyle(InTreatmentFlowBorder, true, isValid);
                    break;

                case "Completed":
                    ApplyFlowBorderStyle(CompletedFlowBorder, true, isValid);
                    break;
            }
        }
        private void ApplyFlowBorderStyle(Border? border, bool isHighlighted, bool isValid)
        {
            if (border == null)
                return;

            if (!isHighlighted)
            {
                border.Background = Brushes.WhiteSmoke;
                border.BorderBrush = Brushes.DarkGray;
                border.BorderThickness = new Thickness(1);
                return;
            }

            if (isValid)
            {
                border.Background = Brushes.Honeydew;
                border.BorderBrush = Brushes.SeaGreen;
                border.BorderThickness = new Thickness(3);
            }
            else
            {
                border.Background = Brushes.MistyRose;
                border.BorderBrush = Brushes.IndianRed;
                border.BorderThickness = new Thickness(3);
            }
        }
        //Prüfen, ob ein Drop fachlich erlaubt ist
        private bool IsValidFlowDropTarget(string sourceColumn, string targetColumn)
        {
            if (string.IsNullOrWhiteSpace(sourceColumn) || string.IsNullOrWhiteSpace(targetColumn))
                return false;

            if (string.Equals(sourceColumn, targetColumn, StringComparison.OrdinalIgnoreCase))
                return false;

            return sourceColumn switch
            {
                "CheckedIn" => targetColumn is "Waiting" or "InTreatment" or "Completed",
                "Waiting" => targetColumn is "CheckedIn" or "InTreatment" or "Completed",
                "InTreatment" => targetColumn is "Waiting" or "Completed",
                "Completed" => false,
                _ => false
            };
        }
        //Hilfsmethoden für Icon und Timer einbauen Datei
        private string GetFlowStatusIcon(string currentColumn)
        {
            return currentColumn switch
            {
                "CheckedIn" => "🔵",
                "Waiting" => "🟢",
                "InTreatment" => "🟡",
                "Completed" => "⚫",
                _ => "⚪"
            };
        }
        private string BuildWaitingTimeText(Appointment appointment, string currentColumn)
        {
            DateTime referenceTime;

            if (appointment.CheckInTime.HasValue)
            {
                referenceTime = appointment.CheckInTime.Value;
            }
            else
            {
                referenceTime = appointment.StartTime;
            }

            var diff = DateTime.Now - referenceTime;

            if (diff.TotalMinutes < 0)
                diff = TimeSpan.Zero;

            var minutes = (int)Math.Floor(diff.TotalMinutes);

            return currentColumn switch
            {
                "CheckedIn" => $"eingecheckt seit {minutes} min",
                "Waiting" => $"wartet seit {minutes} min",
                "InTreatment" => $"in Behandlung seit {minutes} min",
                "Completed" => $"abgeschlossen",
                _ => string.Empty
            };
        }
    }
}
