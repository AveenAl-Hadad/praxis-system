using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Button = System.Windows.Controls.Button;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.MessageBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using System.Windows.Controls.Primitives;
using Praxis.Client.Views;
using Point = System.Windows.Point;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using DataObject = System.Windows.DataObject;
using System.Windows.Input;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Cursors = System.Windows.Input.Cursors;
using Microsoft.Extensions.Logging;
using Praxis.Infrastructure.Services;
using Microsoft.VisualBasic.Logging;

namespace Praxis.Client.Views.Pages.Patienten
{
    public partial class PatientAppointmentsPage : System.Windows.Controls.UserControl
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IRoomService _roomService;
        private readonly IPatientService _patientService;

        private Patient? _currentPatient;
        private Appointment? _selectedAppointment;
        private bool _isLoadingForm;
        private ListBox? _availableSlotsListBox;
        private Point? _plannerDragStartPoint;
        private PlannerResizeState? _plannerResizeState;
        private bool _isWeekMode;
        private DateTime _plannerSelectedDate = DateTime.Today;
        private Point? _flowDragStartPoint;

        public PatientAppointmentsPage(
            IAppointmentService appointmentService,
            IRoomService roomService,
            IPatientService patientService)
        {
            InitializeComponent();

            _appointmentService = appointmentService;
            _roomService = roomService;

            AppointmentDatePicker.SelectedDate = DateTime.Today;
            Loaded += PatientAppointmentsPage_Loaded;
            _patientService = patientService;
        }
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
                var row = (minutesFromStart / slotMinutes) + 1;
                var rowSpan = Math.Max(1, (int)Math.Ceiling(appointment.DurationMinutes / 15.0));

                var column = 1 + (dayOffset * roomNames.Count) + roomIndex;

                var button = CreatePlannerAppointmentButton(appointment);

                Grid.SetRow(button, row);
                Grid.SetColumn(button, column);
                Grid.SetRowSpan(button, rowSpan);

                RoomPlannerGrid.Children.Add(button);
            }
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

            // 🔥 WICHTIG: sichere Variante
            var listBox = AvailableSlotsListBox ?? FindName("AvailableSlotsListBox") as ListBox;

            if (listBox != null)
                listBox.SelectedItem = null;

            _isLoadingForm = false;
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
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
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
        //Neue Hilfsklassen für Drag-Daten Datei
        private sealed class PlannerDropTarget
        {
            public int Row { get; set; }
            public int Column { get; set; }
        }

        private sealed class PlannerDragPayload
        {
            public int AppointmentId { get; set; }
        }
        //Termine in Raster eintragen
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
                var row = (minutesFromStart / slotMinutes) + 1;

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
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left,
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
                var newDuration = _plannerResizeState.OriginalDurationMinutes + (slotDelta * slotMinutes);

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
        private void PlannerAppointmentButton_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsResizeInProgress())
                return;
            if (sender is not Button button)
                return;

            if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
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
        //Drop-Handler einbauen
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
        private async void PreviousDayButton_Click(object sender, RoutedEventArgs e)
        {
            _plannerSelectedDate = _isWeekMode
                ? _plannerSelectedDate.AddDays(-7)
                : _plannerSelectedDate.AddDays(-1);

            AppointmentDatePicker.SelectedDate = _plannerSelectedDate;

            await RefreshAvailableSlotsAsync();
            await RefreshRoomPlannerAsync();
        }
        private async void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            _plannerSelectedDate = DateTime.Today;
            AppointmentDatePicker.SelectedDate = _plannerSelectedDate;

            await RefreshAvailableSlotsAsync();
            await RefreshRoomPlannerAsync();
        }
        private async void NextDayButton_Click(object sender, RoutedEventArgs e)
        {
            _plannerSelectedDate = _isWeekMode
                ? _plannerSelectedDate.AddDays(7)
                : _plannerSelectedDate.AddDays(1);

            AppointmentDatePicker.SelectedDate = _plannerSelectedDate;

            await RefreshAvailableSlotsAsync();
            await RefreshRoomPlannerAsync();
        }
        private async void WeekModeToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            _isWeekMode = true;
            await RefreshRoomPlannerAsync();
        }
        private async void WeekModeToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            _isWeekMode = false;
            await RefreshRoomPlannerAsync();
        }
        private DateTime GetStartOfWeek(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.Date.AddDays(-diff);
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
        private sealed class WeekDropMapping
        {
            public DateTime TargetStartTime { get; set; }
            public string TargetRoomName { get; set; } = string.Empty;
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
        //Hilfsmethoden ergänzen
        private sealed class PlannerResizeState
        {
            public int AppointmentId { get; set; }
            public Point StartPoint { get; set; }
            public int OriginalDurationMinutes { get; set; }
        }
        private int? GetAppointmentIdFromMenuSender(object sender)
        {
            if (sender is not MenuItem menuItem)
                return null;

            if (menuItem.Tag is int appointmentId)
                return appointmentId;

            return null;
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
        }

        private string GetPlannerTitlePrefix(Appointment appointment)
        {
            var status = appointment.Status?.Trim().ToLowerInvariant() ?? string.Empty;
            var treatmentState = appointment.TreatmentState?.Trim().ToLowerInvariant() ?? string.Empty;

            if (status == "abgesagt" || treatmentState == "abgesagt")
                return "[ABGESAGT] ";

            if (treatmentState == "in behandlung")
                return "[BEHANDLUNG] ";

            if (appointment.CheckInTime.HasValue)
                return "[CHECK-IN] ";

            if (status == "bestätigt")
                return "[BESTÄTIGT] ";

            return string.Empty;
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

        private async void AppointmentCriteria_Changed(object sender, RoutedEventArgs e)
        {
            if (AppointmentDatePicker.SelectedDate.HasValue)
            {
                _plannerSelectedDate = AppointmentDatePicker.SelectedDate.Value.Date;
            }

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
        // Status-Text aufbereiten
        private async void RoomPlannerAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            ResetPlannerDragState();

            if (IsResizeInProgress())
                return;

            if (sender is not Button button)
                return;

            if (button.Tag is not int appointmentId)
                return;

            await OpenAppointmentInFormAsync(appointmentId);
        }

        private string BuildPlannerStatusLabel(Appointment appointment)
        {
            var parts = new List<string>();

            var status = string.IsNullOrWhiteSpace(appointment.Status)
                ? "Geplant"
                : appointment.Status.Trim();

            parts.Add(status);

            if (!string.IsNullOrWhiteSpace(appointment.TreatmentState) &&
                !string.Equals(appointment.TreatmentState.Trim(), status, StringComparison.OrdinalIgnoreCase))
            {
                parts.Add(appointment.TreatmentState.Trim());
            }

            if (appointment.CheckInTime.HasValue)
            {
                parts.Add($"Check-in {appointment.CheckInTime.Value:HH:mm}");
            }

            return string.Join(" | ", parts);
        }
        private async Task InitializePlannerFiltersAsync()
        {
            var rooms = await _roomService.GetActiveAsync();
            var patients = await _patientService.GetAllPatientsAsync();

            var roomItems = new List<string> { "Alle Räume" };
            roomItems.AddRange(rooms.OrderBy(r => r.Name).Select(r => r.Name));

            PlannerRoomFilterComboBox.ItemsSource = roomItems;
            PlannerRoomFilterComboBox.SelectedIndex = 0;

            PlannerStatusFilterComboBox.ItemsSource = new List<string>
            {
                "Alle Status",
                "Geplant",
                "Bestätigt",
                "Abgesagt",
                "In Behandlung",
                "Abgeschlossen"
            };
            PlannerStatusFilterComboBox.SelectedIndex = 0;

            var patientItems = new List<PatientFilterItem>
    {
        new PatientFilterItem { Id = 0, FullName = "Alle Patienten" }
    };

            patientItems.AddRange(
                patients
                    .OrderBy(p => p.FullName)
                    .Select(p => new PatientFilterItem
                    {
                        Id = p.Id,
                        FullName = p.FullName
                    }));

            PlannerPatientFilterComboBox.ItemsSource = patientItems;
            PlannerPatientFilterComboBox.SelectedIndex = 0;

            PlannerCheckedInOnlyCheckBox.IsChecked = false;
            PlannerActiveOnlyCheckBox.IsChecked = true;
        }
        // Filtermethoden einbauen
        private List<Appointment> ApplyPlannerFilters(IEnumerable<Appointment> appointments)
        {
            var filtered = appointments.ToList();

            var selectedRoom = PlannerRoomFilterComboBox?.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(selectedRoom) && selectedRoom != "Alle Räume")
            {
                filtered = filtered
                    .Where(a => string.Equals(a.RoomName, selectedRoom, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var selectedStatus = PlannerStatusFilterComboBox?.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(selectedStatus) && selectedStatus != "Alle Status")
            {
                if (string.Equals(selectedStatus, "In Behandlung", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = filtered
                        .Where(a => string.Equals(a.TreatmentState, "In Behandlung", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else
                {
                    filtered = filtered
                        .Where(a => string.Equals(a.Status, selectedStatus, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }

            if (PlannerPatientFilterComboBox?.SelectedItem is PatientFilterItem patientItem && patientItem.Id > 0)
            {
                filtered = filtered
                    .Where(a => a.PatientId == patientItem.Id)
                    .ToList();
            }
            else if (!string.IsNullOrWhiteSpace(PlannerPatientFilterComboBox?.Text) &&
                     !string.Equals(PlannerPatientFilterComboBox.Text.Trim(), "Alle Patienten", StringComparison.OrdinalIgnoreCase))
            {
                var patientSearch = PlannerPatientFilterComboBox.Text.Trim();

                filtered = filtered
                    .Where(a => a.Patient?.FullName?.Contains(patientSearch, StringComparison.OrdinalIgnoreCase) ?? false)
                    .ToList();
            }

            if (PlannerCheckedInOnlyCheckBox?.IsChecked == true)
            {
                filtered = filtered
                    .Where(a => a.CheckInTime.HasValue)
                    .ToList();
            }

            if (PlannerActiveOnlyCheckBox?.IsChecked == true)
            {
                filtered = filtered
                    .Where(a =>
                        !string.Equals(a.Status, "Abgesagt", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(a.Status, "Abgeschlossen", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return filtered;
        }

        // Filter-Events einbauen
        private async void PlannerFilter_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingForm)
                return;

            await RefreshRoomPlannerAsync();
        }

        // Für TextChanged von TextBox brauchst du noch die Überladung:
        private async void PlannerFilter_Changed(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingForm)
                return;

            await RefreshRoomPlannerAsync();
        }
        
        //Filter zurücksetzen
        private async void ResetPlannerFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            _isLoadingForm = true;

            PlannerRoomFilterComboBox.SelectedIndex = 0;
            PlannerStatusFilterComboBox.SelectedIndex = 0;
            PlannerPatientFilterComboBox.SelectedIndex = 0;
            PlannerPatientFilterComboBox.Text = "Alle Patienten";
            PlannerCheckedInOnlyCheckBox.IsChecked = false;
            PlannerActiveOnlyCheckBox.IsChecked = true;

            _isLoadingForm = false;

            await RefreshRoomPlannerAsync();
        }

        //Kennzahlen berechnen
        private async Task<PlannerStatistics> BuildPlannerStatisticsAsync(List<Appointment> filteredAppointments)
        {
            var stats = new PlannerStatistics
            {
                VisibleCount = filteredAppointments.Count,
                ConfirmedCount = filteredAppointments.Count(a =>
                    string.Equals(a.Status, "Bestätigt", StringComparison.OrdinalIgnoreCase)),
                CheckedInCount = filteredAppointments.Count(a => a.CheckInTime.HasValue),
                InTreatmentCount = filteredAppointments.Count(a =>
                    string.Equals(a.TreatmentState, "In Behandlung", StringComparison.OrdinalIgnoreCase)),
                CancelledCount = filteredAppointments.Count(a =>
                    string.Equals(a.Status, "Abgesagt", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a.TreatmentState, "Abgesagt", StringComparison.OrdinalIgnoreCase))
            };

            stats.FreeSlotsCount = await CalculateVisibleFreeSlotsAsync();

            return stats;
        }
        // Frei Sloit zählen
        private async Task<int> CalculateVisibleFreeSlotsAsync()
        {
            if (AppointmentDatePicker.SelectedDate == null)
                return 0;

            if (!int.TryParse(DurationTextBox.Text, out var duration) || duration <= 0)
                duration = 30;

            var activeRooms = await _roomService.GetActiveAsync();
            if (activeRooms.Count == 0)
                return 0;

            var selectedRoom = PlannerRoomFilterComboBox?.SelectedItem?.ToString();

            var roomsToCheck = activeRooms.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(selectedRoom) && selectedRoom != "Alle Räume")
            {
                roomsToCheck = roomsToCheck.Where(r =>
                    string.Equals(r.Name, selectedRoom, StringComparison.OrdinalIgnoreCase));
            }

            int total = 0;
            var selectedDate = AppointmentDatePicker.SelectedDate.Value.Date;

            foreach (var room in roomsToCheck)
            {
                var slots = await _appointmentService.GetAvailableSlotsAsync(selectedDate, duration, room.Name);
                total += slots.Count;
            }

            return total;
        }
        // Kennzahlen in UI schreiben
        private async Task RefreshPlannerStatisticsAsync(List<Appointment> filteredAppointments)
        {
            if (PlannerVisibleCountTextBlock == null ||
                PlannerConfirmedCountTextBlock == null ||
                PlannerCheckedInCountTextBlock == null ||
                PlannerInTreatmentCountTextBlock == null ||
                PlannerCancelledCountTextBlock == null ||
                PlannerFreeSlotsCountTextBlock == null)
            {
                return;
            }

            var stats = await BuildPlannerStatisticsAsync(filteredAppointments);

            PlannerVisibleCountTextBlock.Text = stats.VisibleCount.ToString();
            PlannerConfirmedCountTextBlock.Text = stats.ConfirmedCount.ToString();
            PlannerCheckedInCountTextBlock.Text = stats.CheckedInCount.ToString();
            PlannerInTreatmentCountTextBlock.Text = stats.InTreatmentCount.ToString();
            PlannerCancelledCountTextBlock.Text = stats.CancelledCount.ToString();
            PlannerFreeSlotsCountTextBlock.Text = stats.FreeSlotsCount.ToString();
        }

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
            var state = appointment.TreatmentState?.Trim() ?? "";
            var status = appointment.Status?.Trim() ?? "";

            return appointment.CheckInTime.HasValue &&
                   !string.Equals(state, "Wartet", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(state, "In Behandlung", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(state, "Abgeschlossen", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(status, "Abgeschlossen", StringComparison.OrdinalIgnoreCase);
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
                CurrentColumn = currentColumn
            };
        }

        //Buttons für Statuswechsel  Jetzt kommen die Aktionen.
        // 1) Öffnen
        private async void FlowOpenButton_Click(object sender, RoutedEventArgs e)
        {
           // MessageBox.Show("Button geklickt");
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

        // Hilfsmethoden
        private Brush GetPlannerBackgroundBrush(Appointment appointment)
        {
            var status = appointment.Status?.Trim().ToLowerInvariant() ?? string.Empty;
            var treatmentState = appointment.TreatmentState?.Trim().ToLowerInvariant() ?? string.Empty;

            if (status == "abgesagt" || treatmentState == "abgesagt")
                return Brushes.LightGray;

            if (treatmentState == "in behandlung")
                return Brushes.Khaki;

            if (status == "bestätigt")
                return Brushes.LightGreen;

            return Brushes.WhiteSmoke;
        }

        private Brush GetPlannerBorderBrush(Appointment appointment)
        {
            var status = appointment.Status?.Trim().ToLowerInvariant() ?? string.Empty;
            var treatmentState = appointment.TreatmentState?.Trim().ToLowerInvariant() ?? string.Empty;

            if (status == "abgesagt" || treatmentState == "abgesagt")
                return Brushes.Gray;

            if (treatmentState == "in behandlung")
                return Brushes.Goldenrod;

            if (status == "bestätigt")
                return Brushes.SeaGreen;

            return Brushes.DarkGray;
        }

        private Brush GetPlannerForegroundBrush(Appointment appointment)
        {
            var status = appointment.Status?.Trim().ToLowerInvariant() ?? string.Empty;
            var treatmentState = appointment.TreatmentState?.Trim().ToLowerInvariant() ?? string.Empty;

            if (status == "abgesagt" || treatmentState == "abgesagt")
                return Brushes.DimGray;

            return Brushes.Black;
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
        //Rechtsklick auf Eintrag öffnet Kontexmenü
        private void FlowItemBorder_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border)
                return;

            if (border.Tag is not FlowAppointmentItem item)
                return;

            var menu = BuildFlowContextMenu(item);
            menu.PlacementTarget = border;
            menu.IsOpen = true;

            e.Handled = true;
        }

        // Hilfamethode für Menü-AppointmentId
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
        // Auf Wartet setzen
        private async void FlowMoveToWaitingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var appointmentId = GetAppointmentIdFromFlowMenuSender(sender);
            if (appointmentId == null)
                return;

            await UpdateFlowStateAsync(appointmentId.Value, "Wartet");
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
        private sealed class AvailableSlotItem
        {
            public DateTime SlotTime { get; set; }
            public string SlotLabel { get; set; } = string.Empty;
            public bool IsCurrentAppointmentSlot { get; set; }
        }

        private sealed class PatientFilterItem
        {
            public int Id { get; set; }
            public string FullName { get; set; } = string.Empty;
        }
        private sealed class PlannerStatistics
        {
            public int VisibleCount { get; set; }
            public int ConfirmedCount { get; set; }
            public int CheckedInCount { get; set; }
            public int InTreatmentCount { get; set; }
            public int CancelledCount { get; set; }
            public int FreeSlotsCount { get; set; }
        }
        private sealed class FlowAppointmentItem
        {
            public int AppointmentId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Subtitle { get; set; } = string.Empty;
            public string CurrentColumn { get; set; } = string.Empty;
        }
        private sealed class FlowDragPayload
        {
            public int AppointmentId { get; set; }
            public string SourceColumn { get; set; } = string.Empty;
        }

    }

}