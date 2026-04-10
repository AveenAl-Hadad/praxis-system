using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Praxis.Client.Views;
using Praxis.Domain.Entities;


namespace Praxis.Client.Views.Pages
{
    public partial class DashboardPage : System.Windows.Controls.UserControl
    {
        public DashboardPage()
        {
            InitializeComponent();
            Loaded += DashboardPage_Loaded;
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshAsync();
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
                var openTasks = (await mainWindow.GetOpenDashboardTasksAsync())
                    .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                    .ToList();

                var activeNotices = (await mainWindow.GetActivePracticeNoticesAsync())
                    .ToList();

                // Gesamtzahlen
                TotalPatientsText.Text = stats.TotalPatients.ToString();
                TotalAppointmentsText.Text = stats.TotalAppointments.ToString();
                TotalInvoicesText.Text = stats.TotalInvoices.ToString();
                TotalPrescriptionsText.Text = stats.TotalPrescriptions.ToString();

                // Monatszahlen
                MonthAppointmentsText.Text = stats.CurrentMonthAppointments.ToString();
                MonthInvoicesText.Text = stats.CurrentMonthInvoices.ToString();
                MonthRevenueText.Text = $"{stats.CurrentMonthRevenue:N2} €";
                TotalRevenueText.Text = $"{stats.TotalRevenue:N2} €";

                // Heutige Kennzahlen
                TodayAppointmentsText.Text = todayAppointments.Count.ToString();
                PlannedAppointmentsText.Text = todayAppointments.Count(a =>
                    string.Equals(a.Status, "Geplant", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(a.Status)).ToString();

                CompletedAppointmentsText.Text = todayAppointments.Count(a =>
                    string.Equals(a.Status, "Erledigt", StringComparison.OrdinalIgnoreCase)).ToString();

                CancelledAppointmentsText.Text = todayAppointments.Count(a =>
                    string.Equals(a.Status, "Abgesagt", StringComparison.OrdinalIgnoreCase)).ToString();

                TodayDateText.Text = $"Stand: {DateTime.Now:dd.MM.yyyy HH:mm}";
                // Aufgaben-Kennzahlen
                OpenTasksText.Text = openTasks.Count.ToString();

                DueTodayTasksText.Text = openTasks.Count(t =>
                    t.DueDate != null &&
                    t.DueDate.Value.Date == DateTime.Today).ToString();

                OverdueTasksText.Text = openTasks.Count(t =>
                    t.DueDate != null &&
                    t.DueDate.Value.Date < DateTime.Today).ToString();

                // Hinweise-Kennzahlen
                ActiveNoticesText.Text = activeNotices.Count.ToString();
                PinnedNoticesText.Text = activeNotices.Count(n => n.IsPinned).ToString();

                TodayAppointmentsGrid.ItemsSource = todayAppointments.Select(a => new DashboardAppointmentRow
                {
                    Time = a.StartTime.ToString("HH:mm"),
                    PatientName = a.Patient?.FullName ?? $"Patient #{a.PatientId}",
                    Reason = string.IsNullOrWhiteSpace(a.Reason) ? "-" : a.Reason,
                    Status = string.IsNullOrWhiteSpace(a.Status) ? "Geplant" : a.Status,
                    DurationMinutes = a.DurationMinutes
                }).ToList();

                TasksGrid.ItemsSource = openTasks.Select(t => new DashboardTaskRow
                {
                    Id = t.Id,
                    Title = string.IsNullOrWhiteSpace(t.Title) ? "-" : t.Title,
                    PatientName = t.Patient?.FullName ?? "-",
                    Priority = string.IsNullOrWhiteSpace(t.Priority) ? "Normal" : t.Priority,
                    DueDate = t.DueDate?.ToString("dd.MM.yyyy") ?? "-",
                    Status = string.IsNullOrWhiteSpace(t.Status) ? "Offen" : t.Status,
                    AssignedTo = string.IsNullOrWhiteSpace(t.AssignedTo) ? "-" : t.AssignedTo
                }).ToList();

                NoticesGrid.ItemsSource = activeNotices.Select(n => new DashboardNoticeRow
                {
                    Id = n.Id,
                    Title = string.IsNullOrWhiteSpace(n.Title) ? "-" : n.Title,
                    Category = string.IsNullOrWhiteSpace(n.Category) ? "Info" : n.Category,
                    Content = string.IsNullOrWhiteSpace(n.Content) ? "-" : n.Content,
                    VisibleUntil = n.VisibleUntil?.ToString("dd.MM.yyyy") ?? "-"
                }).ToList();
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

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshAsync();
        }

        private async void OpenPatientsButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            await mainWindow.OpenPatientSearchPageAsync();
        }

        private async void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            var dialog = new TaskEditWindow
            {
                Owner = Window.GetWindow(this)
            };

            var result = dialog.ShowDialog();
            if (result != true || dialog.ResultTask == null)
                return;

            try
            {
                await mainWindow.AddDashboardTaskAsync(dialog.ResultTask);
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Speichern der Aufgabe:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void AddNoticeButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            var dialog = new NoticeEditWindow
            {
                Owner = Window.GetWindow(this)
            };

            var result = dialog.ShowDialog();
            if (result != true || dialog.ResultNotice == null)
                return;

            try
            {
                await mainWindow.AddPracticeNoticeAsync(dialog.ResultNotice);
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Speichern des Hinweises:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void DeactivateNoticeButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            if (NoticesGrid.SelectedItem is not DashboardNoticeRow selectedNotice)
            {
                MessageBox.Show("Bitte zuerst einen Hinweis auswählen.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Hinweis '{selectedNotice.Title}' deaktivieren?",
                "Hinweis deaktivieren",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                await mainWindow.DeactivatePracticeNoticeAsync(selectedNotice.Id);
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Deaktivieren des Hinweises:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private async void CompleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            if (TasksGrid.SelectedItem is not DashboardTaskRow selectedTask)
            {
                MessageBox.Show("Bitte zuerst eine Aufgabe auswählen.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Aufgabe '{selectedTask.Title}' als erledigt markieren?",
                "Aufgabe erledigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                await mainWindow.MarkDashboardTaskAsDoneAsync(selectedTask.Id);
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Abschließen der Aufgabe:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private sealed class DashboardAppointmentRow
        {
            public string Time { get; set; } = string.Empty;
            public string PatientName { get; set; } = string.Empty;
            public string Reason { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int DurationMinutes { get; set; }
        }
        private sealed class DashboardTaskRow
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string PatientName { get; set; } = string.Empty;
            public string Priority { get; set; } = string.Empty;
            public string DueDate { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string AssignedTo { get; set; } = string.Empty;
        }

        private sealed class DashboardNoticeRow
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string VisibleUntil { get; set; } = string.Empty;
        }
    }
}