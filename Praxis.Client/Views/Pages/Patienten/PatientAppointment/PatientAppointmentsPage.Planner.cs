using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Praxis.Domain.Entities;

using Button = System.Windows.Controls.Button;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.MessageBox;
using Point = System.Windows.Point;
using DragEventArgs = System.Windows.DragEventArgs;
using DragDropEffects = System.Windows.DragDropEffects;
using DataObject = System.Windows.DataObject;
using Cursors = System.Windows.Input.Cursors;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using HorizontalAlignment = System.Windows.HorizontalAlignment;


namespace Praxis.Client.Views.Pages.Patienten.PatientAppointment
{
    public partial class PatientAppointmentsPage
    {
        private async Task RefreshRoomPlannerAsync()
        {
            if (RoomPlannerGrid == null)
                return;

            var rooms = await _roomService.GetActiveAsync();
            var roomNames = rooms
                .OrderBy(r => r.Name)
                .Select(r => r.Name)
                .ToList();

            List<Appointment> filteredAppointments;

            if (_isWeekMode)
            {
                var startOfWeek = GetStartOfWeek(_plannerSelectedDate);
                var appointments = await _appointmentService.GetAppointmentsByWeekAsync(startOfWeek);
                filteredAppointments = ApplyPlannerFilters(appointments);

                BuildWeekPlannerGridSkeleton(roomNames, startOfWeek);
                FillWeekPlannerGridAppointments(roomNames, filteredAppointments, startOfWeek);
            }
            else
            {
                var selectedDate = _plannerSelectedDate.Date;
                var appointments = await _appointmentService.GetAppointmentsByDateAsync(selectedDate);
                filteredAppointments = ApplyPlannerFilters(appointments);

                BuildRoomPlannerGridSkeleton(roomNames);
                FillRoomPlannerGridAppointments(roomNames, filteredAppointments);
            }

            await RefreshPlannerStatisticsAsync(filteredAppointments);
        }
        private void BuildWeekPlannerGridSkeleton(List<string> roomNames, DateTime startOfWeek)
        {
            RoomPlannerGrid.Children.Clear();
            RoomPlannerGrid.RowDefinitions.Clear();
            RoomPlannerGrid.ColumnDefinitions.Clear();

            RoomPlannerGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(90)
            });

            for (int day = 0; day < 5; day++)
            {
                foreach (var _ in roomNames)
                {
                    RoomPlannerGrid.ColumnDefinitions.Add(new ColumnDefinition
                    {
                        Width = new GridLength(180)
                    });
                }
            }

            RoomPlannerGrid.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });

            AddPlannerHeaderCell(0, "Zeit");

            int column = 1;
            for (int day = 0; day < 5; day++)
            {
                var date = startOfWeek.AddDays(day);
                foreach (var roomName in roomNames)
                {
                    AddPlannerHeaderCell(column, $"{date:dd.MM}\n{roomName}");
                    column++;
                }
            }

            var start = TimeSpan.FromHours(8);
            var end = TimeSpan.FromHours(18);
            var slotIndex = 0;

            for (var time = start; time < end; time = time.Add(TimeSpan.FromMinutes(15)))
            {
                RoomPlannerGrid.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(52)
                });

                var row = slotIndex + 1;
                AddPlannerTimeCell(row, $"{time:hh\\:mm}");

                for (int col = 1; col < RoomPlannerGrid.ColumnDefinitions.Count; col++)
                {
                    AddPlannerEmptyCell(row, col);
                }

                slotIndex++;
            }
        }
        private void FillWeekPlannerGridAppointments(List<string> roomNames, List<Appointment> appointments, DateTime startOfWeek)
        {
            var dayStart = TimeSpan.FromHours(8);
            const int slotMinutes = 15;

            foreach (var appointment in appointments)
            {
                if (string.IsNullOrWhiteSpace(appointment.RoomName))
                    continue;

                var dayOffset = (appointment.StartTime.Date - startOfWeek.Date).Days;
                if (dayOffset < 0 || dayOffset > 4)
                    continue;

                var roomIndex = roomNames.FindIndex(r =>
                    string.Equals(r, appointment.RoomName, StringComparison.OrdinalIgnoreCase));

                if (roomIndex < 0)
                    continue;

                var startTime = appointment.StartTime.TimeOfDay;
                if (startTime < dayStart)
                    continue;

                var minutesFromStart = (int)(startTime - dayStart).TotalMinutes;
                var row = minutesFromStart / slotMinutes + 1;
                var rowSpan = Math.Max(1, (int)Math.Ceiling(appointment.DurationMinutes / 15.0));

                var column = 1 + dayOffset * roomNames.Count + roomIndex;

                var button = CreatePlannerAppointmentButton(appointment);

                Grid.SetRow(button, row);
                Grid.SetColumn(button, column);
                Grid.SetRowSpan(button, rowSpan);

                RoomPlannerGrid.Children.Add(button);
            }
        }

        // Hilfsmethoden
        //Raster-Grundgerüst aufbauen
        private void BuildRoomPlannerGridSkeleton(List<string> roomNames)
        {
            RoomPlannerGrid.Children.Clear();
            RoomPlannerGrid.RowDefinitions.Clear();
            RoomPlannerGrid.ColumnDefinitions.Clear();

            // Spalte 0 = Zeit
            RoomPlannerGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(110)
            });

            foreach (var _ in roomNames)
            {
                RoomPlannerGrid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(220)
                });
            }

            // Kopfzeile
            RoomPlannerGrid.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });

            AddPlannerHeaderCell(0, "Zeit");

            for (int i = 0; i < roomNames.Count; i++)
            {
                AddPlannerHeaderCell(i + 1, roomNames[i]);
            }

            var start = TimeSpan.FromHours(8);
            var end = TimeSpan.FromHours(18);
            var slotIndex = 0;

            for (var time = start; time < end; time = time.Add(TimeSpan.FromMinutes(15)))
            {
                RoomPlannerGrid.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(52)
                });

                var row = slotIndex + 1;
                AddPlannerTimeCell(row, $"{time:hh\\:mm}");

                for (int roomCol = 0; roomCol < roomNames.Count; roomCol++)
                {
                    AddPlannerEmptyCell(row, roomCol + 1);
                }

                slotIndex++;
            }
        }
        //Kopfzellen
        private void AddPlannerHeaderCell(int column, string text)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8)
            };

            var textBlock = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.Child = textBlock;

            Grid.SetRow(border, 0);
            Grid.SetColumn(border, column);

            RoomPlannerGrid.Children.Add(border);
        }
        //Zeitspalte
        private void AddPlannerTimeCell(int row, string text)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6)
            };

            var textBlock = new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.Medium
            };

            border.Child = textBlock;

            Grid.SetRow(border, row);
            Grid.SetColumn(border, 0);

            RoomPlannerGrid.Children.Add(border);
        }
        //Leere Rasterzellen
        private void AddPlannerEmptyCell(int row, int column)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                AllowDrop = true,
                Tag = new PlannerDropTarget
                {
                    Row = row,
                    Column = column
                }
            };

            border.DragEnter += PlannerCell_DragEnter;
            border.DragOver += PlannerCell_DragOver;
            border.Drop += PlannerCell_Drop;

            Grid.SetRow(border, row);
            Grid.SetColumn(border, column);

            RoomPlannerGrid.Children.Add(border);
        }

        private void FillRoomPlannerGridAppointments(List<string> roomNames, List<Appointment> appointments)
        {
            var dayStart = TimeSpan.FromHours(8);
            var slotMinutes = 15;

            foreach (var appointment in appointments)
            {
                if (string.IsNullOrWhiteSpace(appointment.RoomName))
                    continue;

                var roomIndex = roomNames.FindIndex(r =>
                    string.Equals(r, appointment.RoomName, StringComparison.OrdinalIgnoreCase));

                if (roomIndex < 0)
                    continue;

                var startTime = appointment.StartTime.TimeOfDay;
                if (startTime < dayStart)
                    continue;

                var minutesFromStart = (int)(startTime - dayStart).TotalMinutes;
                var row = minutesFromStart / slotMinutes + 1;

                var rowSpan = Math.Max(1, (int)Math.Ceiling(appointment.DurationMinutes / 15.0));
                var column = roomIndex + 1;

                var button = CreatePlannerAppointmentButton(appointment);

                Grid.SetRow(button, row);
                Grid.SetColumn(button, column);
                Grid.SetRowSpan(button, rowSpan);

                RoomPlannerGrid.Children.Add(button);
            }
        }
        //Termin-Button im Kalender
        // Kalender-Button farbig machen Datei
        // Drag vom Terminblock starten
        private Button CreatePlannerAppointmentButton(Appointment appointment)
        {
            var patientName = appointment.Patient?.FullName ?? $"Patient #{appointment.PatientId}";
            var endTime = appointment.StartTime.AddMinutes(appointment.DurationMinutes);

            var backgroundBrush = GetPlannerBackgroundBrush(appointment);
            var borderBrush = GetPlannerBorderBrush(appointment);
            var foregroundBrush = GetPlannerForegroundBrush(appointment);

            var title = new TextBlock
            {
                Text = $"{GetPlannerTitlePrefix(appointment)}{appointment.StartTime:HH:mm} - {endTime:HH:mm}",
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Foreground = foregroundBrush
            };

            var patient = new TextBlock
            {
                Text = patientName,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = foregroundBrush
            };

            var reason = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(appointment.Reason) ? "Ohne Grund" : appointment.Reason.Trim(),
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = foregroundBrush
            };

            var status = new TextBlock
            {
                Text = BuildPlannerStatusLabel(appointment),
                Margin = new Thickness(0, 4, 0, 0),
                FontStyle = FontStyles.Italic,
                FontWeight = FontWeights.Medium,
                TextWrapping = TextWrapping.Wrap,
                Foreground = foregroundBrush
            };

            var resizeHandle = new Border
            {
                Height = 10,
                Margin = new Thickness(0, 6, 0, 0),
                Background = borderBrush,
                Cursor = Cursors.SizeNS,
                Tag = appointment.Id
            };

            resizeHandle.PreviewMouseLeftButtonDown += ResizeHandle_PreviewMouseLeftButtonDown;
            resizeHandle.PreviewMouseMove += ResizeHandle_PreviewMouseMove;
            resizeHandle.PreviewMouseLeftButtonUp += ResizeHandle_PreviewMouseLeftButtonUp;

            var stack = new StackPanel();
            stack.Children.Add(title);
            stack.Children.Add(patient);
            stack.Children.Add(reason);
            stack.Children.Add(status);
            stack.Children.Add(resizeHandle);

            var button = new Button
            {
                Margin = new Thickness(2),
                Padding = new Thickness(6),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Top,
                Content = stack,
                Tag = appointment.Id,
                Background = backgroundBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(2),
                ContextMenu = BuildPlannerContextMenu(appointment),
                AllowDrop = false
            };

            button.Click += RoomPlannerAppointmentButton_Click;

            var isCancelled =
                string.Equals(appointment.Status, "Abgesagt", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(appointment.TreatmentState, "Abgesagt", StringComparison.OrdinalIgnoreCase);

            if (!isCancelled)
            {
                button.PreviewMouseMove += PlannerAppointmentButton_PreviewMouseMove;
            }

            return button;
        }
        //Resize starten
        private void ResizeHandle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border)
                return;

            if (border.Tag is not int appointmentId)
                return;

            var position = e.GetPosition(this);

            var appointmentTask = _appointmentService.GetAppointmentByIdAsync(appointmentId);
            appointmentTask.Wait();

            var appointment = appointmentTask.Result;
            if (appointment == null)
                return;

            _plannerResizeState = new PlannerResizeState
            {
                AppointmentId = appointmentId,
                StartPoint = position,
                OriginalDurationMinutes = appointment.DurationMinutes
            };

            border.CaptureMouse();
            e.Handled = true;
        }
        //Resize während Ziehen
        private void ResizeHandle_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_plannerResizeState == null)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            e.Handled = true;
        }
        //Resize abschließen
        private async void ResizeHandle_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_plannerResizeState == null)
                    return;

                if (sender is not Border border)
                    return;

                var endPoint = e.GetPosition(this);
                var verticalDelta = endPoint.Y - _plannerResizeState.StartPoint.Y;

                const double plannerRowHeight = 52.0;
                const int slotMinutes = 15;

                var slotDelta = (int)Math.Round(verticalDelta / plannerRowHeight);
                var newDuration = _plannerResizeState.OriginalDurationMinutes + slotDelta * slotMinutes;

                if (newDuration < 15)
                    newDuration = 15;

                var appointment = await _appointmentService.GetAppointmentByIdAsync(_plannerResizeState.AppointmentId);
                if (appointment == null)
                    return;

                if (newDuration == appointment.DurationMinutes)
                    return;

                appointment.DurationMinutes = newDuration;
                var appointmentEnd = appointment.StartTime.AddMinutes(newDuration);
                var plannerDayEnd = appointment.StartTime.Date.AddHours(18);

                if (appointmentEnd > plannerDayEnd)
                {
                    MessageBox.Show("Der Termin darf nicht über das Kalenderende hinausgehen.",
                        "Hinweis",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                await _appointmentService.UpdateAppointmentAsync(appointment);

                await RefreshAppointmentsAsync();
                await RefreshAvailableSlotsAsync();
                await RefreshRoomPlannerAsync();
                await OpenAppointmentInFormAsync(appointment.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (sender is Border border)
                    border.ReleaseMouseCapture();

                _plannerResizeState = null;
            }
        }
        private bool IsResizeInProgress()
        {
            return _plannerResizeState != null;
        }
        //Drag-Start-Handler einbauen      
        private void PlannerAppointmentButton_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (IsResizeInProgress())
                return;
            if (sender is not Button button)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _plannerDragStartPoint = null;
                return;
            }

            var currentPosition = e.GetPosition(this);

            if (_plannerDragStartPoint == null)
            {
                _plannerDragStartPoint = currentPosition;
                return;
            }

            var diff = currentPosition - _plannerDragStartPoint.Value;
            if (Math.Abs(diff.X) < 8 && Math.Abs(diff.Y) < 8)
                return;

            if (button.Tag is not int appointmentId)
                return;

            var payload = new PlannerDragPayload
            {
                AppointmentId = appointmentId
            };

            var data = new DataObject(typeof(PlannerDragPayload), payload);
            DragDrop.DoDragDrop(button, data, DragDropEffects.Move);

            _plannerDragStartPoint = null;
        }
        private void ResetPlannerDragState()
        {
            _plannerDragStartPoint = null;
        }
        private void PlannerCell_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(PlannerDragPayload)))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }
        private void PlannerCell_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(PlannerDragPayload)))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }
        private async void PlannerCell_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (!e.Data.GetDataPresent(typeof(PlannerDragPayload)))
                    return;

                if (sender is not Border border)
                    return;

                if (border.Tag is not PlannerDropTarget dropTarget)
                    return;

                var payload = e.Data.GetData(typeof(PlannerDragPayload)) as PlannerDragPayload;
                if (payload == null)
                    return;

                await MoveAppointmentByDropAsync(payload.AppointmentId, dropTarget);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ResetPlannerDragState();
            }
        }
        //Termin anhand von Rasterposition verschieben
        private async Task MoveAppointmentByDropAsync(int appointmentId, PlannerDropTarget dropTarget)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
            if (appointment == null)
                return;

            var roomNames = (await _roomService.GetActiveAsync())
                .OrderBy(r => r.Name)
                .Select(r => r.Name)
                .ToList();

            DateTime targetStartTime;
            string targetRoomName;

            if (_isWeekMode)
            {
                var startOfWeek = GetStartOfWeek(_plannerSelectedDate);
                var mapped = MapWeekDropTarget(dropTarget, roomNames, startOfWeek);

                targetStartTime = mapped.TargetStartTime;
                targetRoomName = mapped.TargetRoomName;
            }
            else
            {
                if (AppointmentDatePicker.SelectedDate == null)
                    return;

                if (dropTarget.Column <= 0 || dropTarget.Column > roomNames.Count)
                    return;

                targetRoomName = roomNames[dropTarget.Column - 1];
                targetStartTime = BuildPlannerDateTimeFromRow(
                    AppointmentDatePicker.SelectedDate.Value.Date,
                    dropTarget.Row);
            }

            if (targetStartTime == appointment.StartTime &&
                string.Equals(targetRoomName, appointment.RoomName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            appointment.StartTime = targetStartTime;
            appointment.RoomName = targetRoomName;

            await _appointmentService.UpdateAppointmentAsync(appointment);

            await RefreshAppointmentsAsync();
            await RefreshAvailableSlotsAsync();
            await RefreshRoomPlannerAsync();
            await OpenAppointmentInFormAsync(appointmentId);
        }      
        private WeekDropMapping MapWeekDropTarget(PlannerDropTarget dropTarget, List<string> roomNames, DateTime startOfWeek)
        {
            if (dropTarget.Column <= 0)
                throw new InvalidOperationException("Ungültige Zielspalte.");

            var zeroBased = dropTarget.Column - 1;
            var dayIndex = zeroBased / roomNames.Count;
            var roomIndex = zeroBased % roomNames.Count;

            if (dayIndex < 0 || dayIndex > 4)
                throw new InvalidOperationException("Ungültiger Wochentag.");

            if (roomIndex < 0 || roomIndex >= roomNames.Count)
                throw new InvalidOperationException("Ungültiger Zielraum.");

            var targetDate = startOfWeek.AddDays(dayIndex);
            var targetStartTime = BuildPlannerDateTimeFromRow(targetDate, dropTarget.Row);

            return new WeekDropMapping
            {
                TargetStartTime = targetStartTime,
                TargetRoomName = roomNames[roomIndex]
            };
        }
        private DateTime BuildPlannerDateTimeFromRow(DateTime date, int row)
        {
            const int plannerStartHour = 8;
            const int slotMinutes = 15;

            if (row < 1)
                row = 1;

            var minutesFromStart = (row - 1) * slotMinutes;
            return date.Date.AddHours(plannerStartHour).AddMinutes(minutesFromStart);
        }
        private DateTime GetStartOfWeek(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.Date.AddDays(-diff);
        }

    }
   
}