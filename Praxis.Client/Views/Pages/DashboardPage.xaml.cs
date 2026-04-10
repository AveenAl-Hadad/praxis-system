using System.Windows;
using MessageBox = System.Windows.MessageBox;


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

                TodayAppointmentsGrid.ItemsSource = todayAppointments.Select(a => new DashboardAppointmentRow
                {
                    Time = a.StartTime.ToString("HH:mm"),
                    PatientName = a.Patient?.FullName ?? $"Patient #{a.PatientId}",
                    Reason = string.IsNullOrWhiteSpace(a.Reason) ? "-" : a.Reason,
                    Status = string.IsNullOrWhiteSpace(a.Status) ? "Geplant" : a.Status,
                    DurationMinutes = a.DurationMinutes
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

        private sealed class DashboardAppointmentRow
        {
            public string Time { get; set; } = string.Empty;
            public string PatientName { get; set; } = string.Empty;
            public string Reason { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int DurationMinutes { get; set; }
        }
    }
}