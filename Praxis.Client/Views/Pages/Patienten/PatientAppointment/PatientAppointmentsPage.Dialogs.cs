using Praxis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views.Pages.Patienten.PatientAppointment
{
    public partial class PatientAppointmentsPage
    {
        private ContextMenu BuildPlannerContextMenu(Appointment appointment)
        {
            var menu = new ContextMenu();

            var openItem = new MenuItem
            {
                Header = "Termin öffnen",
                Tag = appointment.Id
            };
            openItem.Click += OpenAppointmentMenuItem_Click;
            menu.Items.Add(openItem);

            menu.Items.Add(new Separator());

            var checkInItem = new MenuItem
            {
                Header = "Check-in",
                Tag = appointment.Id,
                IsEnabled = !appointment.CheckInTime.HasValue &&
                            !string.Equals(appointment.Status, "Abgesagt", StringComparison.OrdinalIgnoreCase)
            };
            checkInItem.Click += CheckInMenuItem_Click;
            menu.Items.Add(checkInItem);

            var inTreatmentItem = new MenuItem
            {
                Header = "In Behandlung",
                Tag = appointment.Id,
                IsEnabled = !string.Equals(appointment.Status, "Abgesagt", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(appointment.TreatmentState, "In Behandlung", StringComparison.OrdinalIgnoreCase)
            };
            inTreatmentItem.Click += InTreatmentMenuItem_Click;
            menu.Items.Add(inTreatmentItem);

            var completeItem = new MenuItem
            {
                Header = "Abschließen",
                Tag = appointment.Id,
                IsEnabled = !string.Equals(appointment.Status, "Abgesagt", StringComparison.OrdinalIgnoreCase)
            };
            completeItem.Click += CompleteMenuItem_Click;
            menu.Items.Add(completeItem);

            var cancelItem = new MenuItem
            {
                Header = "Absagen",
                Tag = appointment.Id,
                IsEnabled = !string.Equals(appointment.Status, "Abgesagt", StringComparison.OrdinalIgnoreCase)
            };
            cancelItem.Click += CancelMenuItem_Click;
            menu.Items.Add(cancelItem);

            menu.Items.Add(new Separator());

            var moveRoomItem = new MenuItem
            {
                Header = "In anderen Raum verschieben",
                Tag = appointment.Id,
                IsEnabled = !string.Equals(appointment.Status, "Abgesagt", StringComparison.OrdinalIgnoreCase)
            };
            moveRoomItem.Click += MoveRoomMenuItem_Click;
            menu.Items.Add(moveRoomItem);

            return menu;
        }
        
        //Termin öffnen
        private async void OpenAppointmentMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var appointmentId = GetAppointmentIdFromMenuSender(sender);
            if (appointmentId == null)
                return;

            await OpenAppointmentInFormAsync(appointmentId.Value);
        }
        private int? GetAppointmentIdFromMenuSender(object sender)
        {
            if (sender is not MenuItem menuItem)
                return null;

            if (menuItem.Tag is int appointmentId)
                return appointmentId;

            return null;
        }
        
        // Kontextmenü dynamisch bauen Datei
        private ContextMenu BuildFlowContextMenu(FlowAppointmentItem item)
        {
            var menu = new ContextMenu();

            var openItem = new MenuItem
            {
                Header = "Termin öffnen",
                Tag = item.AppointmentId
            };
            openItem.Click += FlowOpenMenuItem_Click;
            menu.Items.Add(openItem);

            menu.Items.Add(new Separator());

            var checkInItem = new MenuItem
            {
                Header = "Check-in",
                Tag = item.AppointmentId,
                IsEnabled = item.CurrentColumn != "Completed"
            };
            checkInItem.Click += FlowCheckInMenuItem_Click;
            menu.Items.Add(checkInItem);

            var waitingItem = new MenuItem
            {
                Header = "Auf Wartet setzen",
                Tag = item.AppointmentId,
                IsEnabled = item.CurrentColumn != "Waiting" && item.CurrentColumn != "Completed"
            };
            waitingItem.Click += FlowMoveToWaitingMenuItem_Click;
            menu.Items.Add(waitingItem);

            var inTreatmentItem = new MenuItem
            {
                Header = "In Behandlung",
                Tag = item.AppointmentId,
                IsEnabled = item.CurrentColumn != "InTreatment" && item.CurrentColumn != "Completed"
            };
            inTreatmentItem.Click += FlowMoveToInTreatmentMenuItem_Click;
            menu.Items.Add(inTreatmentItem);

            var completeItem = new MenuItem
            {
                Header = "Abschließen",
                Tag = item.AppointmentId,
                IsEnabled = item.CurrentColumn != "Completed"
            };
            completeItem.Click += FlowCompleteMenuItem_Click;
            menu.Items.Add(completeItem);

            var cancelItem = new MenuItem
            {
                Header = "Absagen",
                Tag = item.AppointmentId,
                IsEnabled = item.CurrentColumn != "Completed"
            };
            cancelItem.Click += FlowCancelMenuItem_Click;
            menu.Items.Add(cancelItem);

            menu.Items.Add(new Separator());

            var moveRoomItem = new MenuItem
            {
                Header = "In anderen Raum verschieben",
                Tag = item.AppointmentId,
                IsEnabled = item.CurrentColumn != "Completed"
            };
            moveRoomItem.Click += FlowMoveRoomMenuItem_Click;
            menu.Items.Add(moveRoomItem);

            return menu;
        }
        private void FlowContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is not ContextMenu menu)
                return;

            if (menu.PlacementTarget is not Border border)
                return;

            if (border.Tag is not FlowAppointmentItem item)
                return;

            var builtMenu = BuildFlowContextMenu(item);

            menu.Items.Clear();
            foreach (var entry in builtMenu.Items)
            {
                menu.Items.Add(entry);
            }
        }
        private int? GetAppointmentIdFromFlowMenuSender(object sender)
        {
            if (sender is not MenuItem menuItem)
                return null;

            if (menuItem.Tag is int appointmentId)
                return appointmentId;

            return null;
        }

        //Kontextmenü-Aktion einbauen
        // Termin öffnen
        private async void FlowOpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var appointmentId = GetAppointmentIdFromFlowMenuSender(sender);
            if (appointmentId == null)
                return;

            await OpenAppointmentInFormAsync(appointmentId.Value);
        }

        // Auf Wartet setzen
        private async void FlowMoveToWaitingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var appointmentId = GetAppointmentIdFromFlowMenuSender(sender);
            if (appointmentId == null)
                return;

            await UpdateFlowStateAsync(appointmentId.Value, "Wartet");
        }
        
        // Check-in
        private async void FlowCheckInMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var appointmentId = GetAppointmentIdFromFlowMenuSender(sender);
            if (appointmentId == null)
                return;

            try
            {
                await _appointmentService.CheckInAsync(appointmentId.Value);

                await RefreshAppointmentsAsync();
                await RefreshAvailableSlotsAsync();
                await RefreshRoomPlannerAsync();
                await RefreshPatientFlowAsync();
                await OpenAppointmentInFormAsync(appointmentId.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // In Behandlung
        private async void FlowMoveToInTreatmentMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var appointmentId = GetAppointmentIdFromFlowMenuSender(sender);
            if (appointmentId == null)
                return;

            await UpdateFlowStateAsync(appointmentId.Value, "In Behandlung");
        }

        // Abschließen
        private async void FlowCompleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var appointmentId = GetAppointmentIdFromFlowMenuSender(sender);
            if (appointmentId == null)
                return;

            await CompleteFlowAppointmentAsync(appointmentId.Value);
        }

        //Absagen
        private async void FlowCancelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var appointmentId = GetAppointmentIdFromFlowMenuSender(sender);
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
                await _appointmentService.CancelAppointmentAsync(appointmentId.Value, "Abgesagt aus Warteliste");

                await RefreshAppointmentsAsync();
                await RefreshAvailableSlotsAsync();
                await RefreshRoomPlannerAsync();
                await RefreshPatientFlowAsync();
                await OpenAppointmentInFormAsync(appointmentId.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // Raum wechslen
        private async void FlowMoveRoomMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var appointmentId = GetAppointmentIdFromFlowMenuSender(sender);
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
                await RefreshPatientFlowAsync();
                await OpenAppointmentInFormAsync(appointmentId.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
