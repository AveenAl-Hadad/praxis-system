using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using System.Collections.ObjectModel;
using Point = System.Windows.Point;

using Button = System.Windows.Controls.Button;
using ListBox = System.Windows.Controls.ListBox;  
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace Praxis.Client.Views.Pages.Patienten.PatientAppointment
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
    private readonly DispatcherTimer _flowRefreshTimer = new DispatcherTimer();

    private readonly IAppointmentMedicalEntryService _appointmentMedicalEntryService;

    private readonly ObservableCollection<AppointmentMedicalEntryRow> _appointmentMedicalEntries = new();

    private CatalogItem? _selectedAppointmentDiagnosis;
    private CatalogItem? _selectedAppointmentService;

    public PatientAppointmentsPage(
                                    IAppointmentService appointmentService,
                                    IRoomService roomService,
                                    IPatientService patientService,
                                    IAppointmentMedicalEntryService appointmentMedicalEntryService)
    {
        InitializeComponent();

        _appointmentService = appointmentService;
        _roomService = roomService;
        _appointmentMedicalEntryService = appointmentMedicalEntryService;
        AppointmentMedicalEntriesGrid.ItemsSource = _appointmentMedicalEntries;

        AppointmentDatePicker.SelectedDate = DateTime.Today;
        Loaded += PatientAppointmentsPage_Loaded;
        _patientService = patientService;
        _flowRefreshTimer.Interval = TimeSpan.FromMinutes(1);
        _flowRefreshTimer.Tick += FlowRefreshTimer_Tick;
        _flowRefreshTimer.Start();

    }
    private async void FlowRefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (!IsLoaded)
            return;

        await RefreshPatientFlowAsync();
    }       
       
    //Drop-Handler einbauen
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

    //==================Sealed Klasse =====================
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
        public string StatusIcon { get; set; } = string.Empty;
        public string WaitingTimeText { get; set; } = string.Empty;
    }
    private sealed class FlowDragPayload
    {
        public int AppointmentId { get; set; }
        public string SourceColumn { get; set; } = string.Empty;
    }
    private sealed class WeekDropMapping
        {
            public DateTime TargetStartTime { get; set; }
            public string TargetRoomName { get; set; } = string.Empty;
        }
    private sealed class PlannerResizeState
        {
            public int AppointmentId { get; set; }
            public Point StartPoint { get; set; }
            public int OriginalDurationMinutes { get; set; }
        }
    private sealed class PlannerDropTarget
        {
            public int Row { get; set; }
            public int Column { get; set; }
        }
    private sealed class PlannerDragPayload
        {
            public int AppointmentId { get; set; }
        }
    }
public class AppointmentMedicalEntryRow
{
    public int Id { get; set; }

    public string DiagnosisText { get; set; } = string.Empty;

    public string ServiceText { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}
public class AppointmentCatalogSuggestion
{
    public CatalogItem Item { get; set; } = null!;

    public string DisplayText => $"{Item.Code} - {Item.Name}";
}

}