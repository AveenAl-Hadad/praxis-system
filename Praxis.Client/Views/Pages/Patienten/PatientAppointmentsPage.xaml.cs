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
            await RefreshRoomPlannerAsync();
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
            await RefreshRoomPlannerAsync();
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
            if (RoomPlannerGrid == null || AppointmentDatePicker == null)
                return;

            if (AppointmentDatePicker.SelectedDate == null)
                return;

            var selectedDate = AppointmentDatePicker.SelectedDate.Value.Date;
            var rooms = await _roomService.GetActiveAsync();
            var appointments = await _appointmentService.GetAppointmentsByDateAsync(selectedDate);

            BuildRoomPlannerGridSkeleton(rooms.Select(r => r.Name).ToList());
            FillRoomPlannerGridAppointments(rooms.Select(r => r.Name).ToList(), appointments);
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
                Padding = new Thickness(0)
            };

            Grid.SetRow(border, row);
            Grid.SetColumn(border, column);

            RoomPlannerGrid.Children.Add(border);
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
        private Button CreatePlannerAppointmentButton(Appointment appointment)
        {
            var patientName = appointment.Patient?.FullName ?? $"Patient #{appointment.PatientId}";
            var endTime = appointment.StartTime.AddMinutes(appointment.DurationMinutes);

            var title = new TextBlock
            {
                Text = $"{appointment.StartTime:HH:mm} - {endTime:HH:mm}",
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            };

            var patient = new TextBlock
            {
                Text = patientName,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };

            var reason = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(appointment.Reason) ? "Ohne Grund" : appointment.Reason,
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };

            var status = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(appointment.Status) ? "Geplant" : appointment.Status,
                Margin = new Thickness(0, 2, 0, 0),
                FontStyle = FontStyles.Italic,
                TextWrapping = TextWrapping.Wrap
            };

            var stack = new StackPanel();
            stack.Children.Add(title);
            stack.Children.Add(patient);
            stack.Children.Add(reason);
            stack.Children.Add(status);

            var button = new Button
            {
                Margin = new Thickness(2),
                Padding = new Thickness(6),
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Top,
                Content = stack,
                Tag = appointment.Id
            };

            button.Click += RoomPlannerAppointmentButton_Click;

            return button;
        }
        //
       
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
            await RefreshAvailableSlotsAsync();
            await RefreshRoomPlannerAsync();
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
        private async void RoomPlannerAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.Tag is not int appointmentId)
                return;

            var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
            if (appointment == null)
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

        private sealed class AvailableSlotItem
        {
            public DateTime SlotTime { get; set; }
            public string SlotLabel { get; set; } = string.Empty;
            public bool IsCurrentAppointmentSlot { get; set; }
        }

    
    }

}