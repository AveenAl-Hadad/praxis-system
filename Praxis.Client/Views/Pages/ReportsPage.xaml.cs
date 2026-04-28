using System.Windows;
using System.Windows.Controls;
using Praxis.Application.Interfaces;

namespace Praxis.Client.Views.Pages;

public partial class ReportsPage : System.Windows.Controls.UserControl
{
    private readonly IReportsService _reportsService;

    public ReportsPage(IReportsService reportsService)
    {
        InitializeComponent();

        _reportsService = reportsService;

        FromDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        ToDatePicker.SelectedDate = DateTime.Today;
    }

    public async Task RefreshAsync()
    {
        var from = FromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-1);
        var to = ToDatePicker.SelectedDate ?? DateTime.Today;

        var summary = await _reportsService.GetSummaryAsync(from, to);

        PatientsText.Text = summary.PatientCount.ToString();
        AppointmentsText.Text = summary.AppointmentCount.ToString();
        DiagnosesText.Text = summary.DiagnosisCount.ToString();
        InvoicesText.Text = summary.InvoiceCount.ToString();
        RevenueText.Text = summary.Revenue.ToString("C");

        DiagnosisGrid.ItemsSource = await _reportsService.GetDiagnosisStatsAsync(from, to);
        InvoiceGrid.ItemsSource = await _reportsService.GetInvoiceStatsAsync(from, to);
        AppointmentGrid.ItemsSource = await _reportsService.GetAppointmentStatsAsync(from, to);
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }
}