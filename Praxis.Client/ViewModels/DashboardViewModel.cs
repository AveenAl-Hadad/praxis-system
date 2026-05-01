using Praxis.Client.Commands;
using Praxis.Client.Views;
using System.Collections.ObjectModel;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        public RelayCommand RefreshCommand { get; }

        public DashboardViewModel()
        {
            RefreshCommand = new RelayCommand(async _ => await RefreshAsync());
            _ = RefreshAsync();
        }

        private int _totalPatients;
        public int TotalPatients
        {
            get => _totalPatients;
            set { _totalPatients = value; OnPropertyChanged(); }
        }

        private int _totalAppointments;
        public int TotalAppointments
        {
            get => _totalAppointments;
            set { _totalAppointments = value; OnPropertyChanged(); }
        }

        private int _totalInvoices;
        public int TotalInvoices
        {
            get => _totalInvoices;
            set { _totalInvoices = value; OnPropertyChanged(); }
        }

        private int _totalPrescriptions;
        public int TotalPrescriptions
        {
            get => _totalPrescriptions;
            set { _totalPrescriptions = value; OnPropertyChanged(); }
        }

        private string _monthAppointments = "0";
        public string MonthAppointments
        {
            get => _monthAppointments;
            set { _monthAppointments = value; OnPropertyChanged(); }
        }

        private string _monthInvoices = "0";
        public string MonthInvoices
        {
            get => _monthInvoices;
            set { _monthInvoices = value; OnPropertyChanged(); }
        }

        private string _monthRevenue = "0,00 €";
        public string MonthRevenue
        {
            get => _monthRevenue;
            set { _monthRevenue = value; OnPropertyChanged(); }
        }

        private string _totalRevenue = "0,00 €";
        public string TotalRevenue
        {
            get => _totalRevenue;
            set { _totalRevenue = value; OnPropertyChanged(); }
        }

        private string _todayDate = "";
        public string TodayDate
        {
            get => _todayDate;
            set { _todayDate = value; OnPropertyChanged(); }
        }
        private int _todayAppointmentsCount;
        public int TodayAppointmentsCount
        {
            get => _todayAppointmentsCount;
            set { _todayAppointmentsCount = value; OnPropertyChanged(); }
        }

        private int _plannedAppointments;
        public int PlannedAppointments
        {
            get => _plannedAppointments;
            set { _plannedAppointments = value; OnPropertyChanged(); }
        }

        private int _completedAppointments;
        public int CompletedAppointments
        {
            get => _completedAppointments;
            set { _completedAppointments = value; OnPropertyChanged(); }
        }

        private int _cancelledAppointments;
        public int CancelledAppointments
        {
            get => _cancelledAppointments;
            set { _cancelledAppointments = value; OnPropertyChanged(); }
        }

        private int _onlineAppointmentsCount;
        public int OnlineAppointmentsCount
        {
            get => _onlineAppointmentsCount;
            set { _onlineAppointmentsCount = value; OnPropertyChanged(); }
        }

        private int _openTasks;
        public int OpenTasks
        {
            get => _openTasks;
            set { _openTasks = value; OnPropertyChanged(); }
        }

        private int _dueTodayTasks;
        public int DueTodayTasks
        {
            get => _dueTodayTasks;
            set { _dueTodayTasks = value; OnPropertyChanged(); }
        }

        private int _overdueTasks;
        public int OverdueTasks
        {
            get => _overdueTasks;
            set { _overdueTasks = value; OnPropertyChanged(); }
        }

        private int _completedTasks;
        public int CompletedTasks
        {
            get => _completedTasks;
            set { _completedTasks = value; OnPropertyChanged(); }
        }
       

        public ObservableCollection<DashboardAppointmentRow> TodayAppointments { get; } = new();
        public ObservableCollection<DashboardAppointmentRow> OnlineAppointments { get; } = new();
        public ObservableCollection<DashboardTaskRow> Tasks { get; } = new();

        public ObservableCollection<DashboardTaskRow> KanbanOpenTasks { get; } = new();
        public ObservableCollection<DashboardTaskRow> KanbanTodayTasks { get; } = new();
        public ObservableCollection<DashboardTaskRow> KanbanOverdueTasks { get; } = new();
        public ObservableCollection<DashboardTaskRow> KanbanCompletedTasks { get; } = new();
        public ObservableCollection<PracticeNoticeRow> Notices { get; } = new();
        public ObservableCollection<PracticeNoticeRow> NoticeCards { get; } = new();

        private int _activeNotices;
        public int ActiveNotices
        {
            get => _activeNotices;
            set { _activeNotices = value; OnPropertyChanged(); }
        }

        private int _pinnedNotices;
        public int PinnedNotices
        {
            get => _pinnedNotices;
            set { _pinnedNotices = value; OnPropertyChanged(); }
        }

        public async Task RefreshAsync()
        {
            try
            {
                if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                    return;

                var stats = await mainWindow.GetDashboardStatsAsync();

                var todayAppointments = (await mainWindow.GetAppointmentsByDateAsync(DateTime.Today))
                    .OrderBy(a => a.StartTime)
                    .ToList();

                var todayOnlineAppointments = todayAppointments
                    .Where(a => a.IsOnlineBooking)
                    .ToList();

                var allTasks = (await mainWindow.GetAllDashboardTasksAsync())
                    .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                    .ThenByDescending(t => t.CreatedAt)
                    .ToList();

                var today = DateTime.Today;

                var completedTasks = allTasks
                    .Where(t => string.Equals(t.Status, "Erledigt", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var overdueTasks = allTasks
                    .Where(t => t.DueDate != null &&
                                t.DueDate.Value.Date < today &&
                                !string.Equals(t.Status, "Erledigt", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var dueTodayTasks = allTasks
                    .Where(t => t.DueDate != null &&
                                t.DueDate.Value.Date == today &&
                                !string.Equals(t.Status, "Erledigt", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var plainOpenTasks = allTasks
                    .Where(t =>
                        !string.Equals(t.Status, "Erledigt", StringComparison.OrdinalIgnoreCase) &&
                        !(t.DueDate != null && t.DueDate.Value.Date < today) &&
                        !(t.DueDate != null && t.DueDate.Value.Date == today))
                    .ToList();

                var activeNotices = (await mainWindow.GetActivePracticeNoticesAsync()).ToList();

                // 🔥 Gesamtzahlen
                TotalPatients = stats.TotalPatients;
                TotalAppointments = stats.TotalAppointments;
                TotalInvoices = stats.TotalInvoices;
                TotalPrescriptions = stats.TotalPrescriptions;

                // 🔥 Monatszahlen
                MonthAppointments = stats.CurrentMonthAppointments.ToString();
                MonthInvoices = stats.CurrentMonthInvoices.ToString();
                MonthRevenue = $"{stats.CurrentMonthRevenue:N2} €";
                TotalRevenue = $"{stats.TotalRevenue:N2} €";

                // 🔥 Heute
                TodayDate = $"Stand: {DateTime.Now:dd.MM.yyyy HH:mm}";

                TodayAppointmentsCount = todayAppointments.Count;
                PlannedAppointments = todayAppointments.Count(a =>
                    string.Equals(a.Status, "Geplant", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(a.Status));

                CompletedAppointments = todayAppointments.Count(a =>
                    string.Equals(a.Status, "Erledigt", StringComparison.OrdinalIgnoreCase));

                CancelledAppointments = todayAppointments.Count(a =>
                    string.Equals(a.Status, "Abgesagt", StringComparison.OrdinalIgnoreCase));

                OnlineAppointmentsCount = todayOnlineAppointments.Count;

                // 🔥 Task-Zähler
                OpenTasks = plainOpenTasks.Count;
                DueTodayTasks = dueTodayTasks.Count;
                OverdueTasks = overdueTasks.Count;
                CompletedTasks = completedTasks.Count;

                // 🔥 Tasks Liste
                Tasks.Clear();
                foreach (var task in allTasks.Select(MapTaskRow))
                    Tasks.Add(task);

                // 🔥 Kanban
                KanbanOpenTasks.Clear();
                foreach (var task in plainOpenTasks.Take(6).Select(MapTaskRow))
                    KanbanOpenTasks.Add(task);

                KanbanTodayTasks.Clear();
                foreach (var task in dueTodayTasks.Take(6).Select(MapTaskRow))
                    KanbanTodayTasks.Add(task);

                KanbanOverdueTasks.Clear();
                foreach (var task in overdueTasks.Take(6).Select(MapTaskRow))
                    KanbanOverdueTasks.Add(task);

                KanbanCompletedTasks.Clear();
                foreach (var task in completedTasks.Take(6).Select(MapTaskRow))
                    KanbanCompletedTasks.Add(task);

                // 🔥 Heute Termine
                TodayAppointments.Clear();
                foreach (var a in todayAppointments)
                {
                    TodayAppointments.Add(new DashboardAppointmentRow
                    {
                        Time = a.StartTime.ToString("HH:mm"),
                        PatientName = a.Patient?.FullName ?? $"Patient #{a.PatientId}",
                        Reason = string.IsNullOrWhiteSpace(a.Reason) ? "-" : a.Reason,
                        Status = string.IsNullOrWhiteSpace(a.Status) ? "Geplant" : a.Status,
                        DurationMinutes = a.DurationMinutes
                    });
                }

                // 🔥 Online Termine
                OnlineAppointments.Clear();
                foreach (var a in todayOnlineAppointments)
                {
                    OnlineAppointments.Add(new DashboardAppointmentRow
                    {
                        Time = a.StartTime.ToString("HH:mm"),
                        PatientName = a.Patient?.FullName ?? $"Patient #{a.PatientId}",
                        Reason = string.IsNullOrWhiteSpace(a.Reason) ? "-" : a.Reason,
                        Status = string.IsNullOrWhiteSpace(a.Status) ? "Geplant" : a.Status,
                        DurationMinutes = a.DurationMinutes
                    });
                }

                ActiveNotices = activeNotices.Count;
                PinnedNotices = activeNotices.Count(n => n.IsPinned);

                var noticeRows = activeNotices.Select(n =>
                {
                    var category = string.IsNullOrWhiteSpace(n.Category) ? "Info" : n.Category;

                    return new PracticeNoticeRow
                    {
                        Id = n.Id,
                        Title = string.IsNullOrWhiteSpace(n.Title) ? "-" : n.Title,
                        Content = string.IsNullOrWhiteSpace(n.Content) ? "-" : n.Content,
                        Category = category,
                        CategoryColor = GetNoticeCategoryColor(category),
                        VisibleUntilText = n.VisibleUntil?.ToString("dd.MM.yyyy") ?? "-",
                        IsActive = n.IsActive,
                        IsPinned = n.IsPinned
                    };
                }).ToList();

                Notices.Clear();
                foreach (var notice in noticeRows)
                    Notices.Add(notice);

                NoticeCards.Clear();
                foreach (var notice in noticeRows)
                    NoticeCards.Add(notice);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Laden des Dashboards:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private string GetPriorityColor(string priority)
        {
            return priority switch
            {
                "Hoch" => "#DC2626",
                "Normal" => "#2563EB",
                "Niedrig" => "#16A34A",
                _ => "#6B7280"
            };
        }
        private string GetTaskStatusColor(string status)
        {
            return status switch
            {
                "Erledigt" => "#6B7280",
                "In Bearbeitung" => "#F59E0B",
                "Offen" => "#2563EB",
                _ => "#6B7280"
            };
        }
        private string GetDueDateColor(bool isOverdue, bool isDueToday, bool isCompleted)
        {
            if (isCompleted)
                return "#6B7280";

            if (isOverdue)
                return "#DC2626";

            if (isDueToday)
                return "#D97706";

            return "#374151";
        }
        private DashboardTaskRow MapTaskRow(dynamic t)
        {
            var priority = string.IsNullOrWhiteSpace(t.Priority) ? "Normal" : t.Priority;
            var status = string.IsNullOrWhiteSpace(t.Status) ? "Offen" : t.Status;

            var isCompleted = string.Equals(status, "Erledigt", StringComparison.OrdinalIgnoreCase);
            var isDueToday =
                t.DueDate != null &&
                t.DueDate.Date == DateTime.Today &&
                !isCompleted;

            var isOverdue =
                t.DueDate != null &&
                t.DueDate.Date < DateTime.Today &&
                !isCompleted;

            return new DashboardTaskRow
            {
                Id = t.Id,
                Title = string.IsNullOrWhiteSpace(t.Title) ? "-" : t.Title,
                PatientName = t.Patient?.FullName ?? "-",
                Priority = priority,
                DueDate = t.DueDate?.ToString("dd.MM.yyyy") ?? "-",
                Status = status,
                AssignedTo = string.IsNullOrWhiteSpace(t.AssignedTo) ? "-" : t.AssignedTo,
                PriorityColor = GetPriorityColor(priority),
                StatusColor = GetTaskStatusColor(status),
                IsCompleted = isCompleted,
                IsDueToday = isDueToday,
                IsOverdue = isOverdue,
                DueDateColor = GetDueDateColor(isOverdue, isDueToday, isCompleted),
                Subtitle = $"{(t.DueDate?.ToString("dd.MM.yyyy") ?? "kein Datum")} · {(string.IsNullOrWhiteSpace(t.AssignedTo) ? "Nicht zugewiesen" : t.AssignedTo)}"
            };
        }

        private string GetNoticeCategoryColor(string category)
        {
            return category switch
            {
                "Wichtig" => "#DC2626",
                "Info" => "#2563EB",
                "Organisation" => "#7C3AED",
                "Labor" => "#16A34A",
                "Abrechnung" => "#F59E0B",
                _ => "#6B7280"
            };
        }
    }
}