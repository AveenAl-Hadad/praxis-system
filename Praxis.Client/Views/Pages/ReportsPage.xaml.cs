using System.Windows;
using System.Windows.Controls;
using Praxis.Application.Interfaces;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Praxis.Domain.Entities;

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

        var diagnosisStats = await _reportsService.GetDiagnosisStatsAsync(from, to);
        var invoiceStats = await _reportsService.GetInvoiceStatsAsync(from, to);
        var appointmentStats = await _reportsService.GetAppointmentStatsAsync(from, to);

        DiagnosisGrid.ItemsSource = diagnosisStats;
        InvoiceGrid.ItemsSource = invoiceStats;
        AppointmentGrid.ItemsSource = appointmentStats;

        LoadCharts(diagnosisStats, invoiceStats);
    }
    private void LoadCharts(List<ReportRow> diagnosisStats, List<ReportRow> invoiceStats)
    {
        DiagnosisChart.Series = new ISeries[]
        {
        new ColumnSeries<int>
        {
            Values = diagnosisStats.Select(x => x.Count).ToArray(),
            Name = "Diagnosen"
        }
        };

        DiagnosisChart.XAxes = new[]
        {
        new Axis
        {
            Labels = diagnosisStats.Select(x => x.Name).ToArray(),
            LabelsRotation = 35
        }
    };

        RevenueChart.Series = new ISeries[]
        {
        new LineSeries<decimal>
        {
            Values = invoiceStats.Select(x => x.Amount).ToArray(),
            Name = "Umsatz"
        }
        };

        RevenueChart.XAxes = new[]
        {
        new Axis
        {
            Labels = invoiceStats.Select(x => x.Name).ToArray(),
            LabelsRotation = 35
        }
    };
    }
    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    public async void ShowOverview()
    {
        ReportsTabControl.SelectedIndex = 0;
        await RefreshAsync();
    }

    public async void ShowDiagnosisStats()
    {
        ReportsTabControl.SelectedIndex = 0;
        await RefreshAsync();
    }

    public async void ShowInvoiceStats()
    {
        ReportsTabControl.SelectedIndex = 1;
        await RefreshAsync();
    }

    public async void ShowAppointmentStats()
    {
        ReportsTabControl.SelectedIndex = 2;
        await RefreshAsync();
    }

    public async void ShowCharts()
    {
        ReportsTabControl.SelectedIndex = 3;
        await RefreshAsync();
    }
}