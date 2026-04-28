using System.Windows;
using System.Windows.Controls;
using Praxis.Application.Interfaces;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Praxis.Domain.Entities;
using Microsoft.Win32;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using ClosedXML.Excel;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;


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
        PatientsWithoutCardGrid.ItemsSource = await _reportsService.GetPatientsWithoutCardAsync();
        ServiceCodeGrid.ItemsSource = await _reportsService.GetServiceCodeStatsAsync(from, to);
        PatientStatsGrid.ItemsSource = await _reportsService.GetPatientStatsAsync();
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
    public async void ShowPatientsWithoutCard()
    {
        ReportsTabControl.SelectedIndex = 4;
        await RefreshAsync();
    }

    public async void ShowServiceCodeStats()
    {
        ReportsTabControl.SelectedIndex = 5;
        await RefreshAsync();
    }

    public async void ShowPatientStats()
    {
        ReportsTabControl.SelectedIndex = 6;
        await RefreshAsync();
    }
    //Pdf export
    private async void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var from = FromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-1);
        var to = ToDatePicker.SelectedDate ?? DateTime.Today;

        var summary = await _reportsService.GetSummaryAsync(from, to);
        var services = await _reportsService.GetServiceCodeStatsAsync(from, to);
        var patientsWithoutCard = await _reportsService.GetPatientsWithoutCardAsync();

        var dialog = new SaveFileDialog
        {
            Filter = "PDF-Datei (*.pdf)|*.pdf",
            FileName = $"Auswertung_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
        };

        if (dialog.ShowDialog() != true)
            return;

        var document = new PdfDocument();
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);

        var titleFont = new XFont("Arial", 18, XFontStyle.Bold);
        var headerFont = new XFont("Arial", 12, XFontStyle.Bold);
        var textFont = new XFont("Arial", 10, XFontStyle.Regular);

        double y = 40;

        gfx.DrawString("Praxis-Auswertung", titleFont, XBrushes.Black, 40, y);
        y += 35;

        gfx.DrawString($"Zeitraum: {from:dd.MM.yyyy} - {to:dd.MM.yyyy}", textFont, XBrushes.Black, 40, y);
        y += 25;

        gfx.DrawString("Übersicht", headerFont, XBrushes.Black, 40, y);
        y += 20;

        gfx.DrawString($"Patienten: {summary.PatientCount}", textFont, XBrushes.Black, 40, y); y += 16;
        gfx.DrawString($"Termine: {summary.AppointmentCount}", textFont, XBrushes.Black, 40, y); y += 16;
        gfx.DrawString($"Diagnosen: {summary.DiagnosisCount}", textFont, XBrushes.Black, 40, y); y += 16;
        gfx.DrawString($"Rechnungen: {summary.InvoiceCount}", textFont, XBrushes.Black, 40, y); y += 16;
        gfx.DrawString($"Umsatz: {summary.Revenue:C}", textFont, XBrushes.Black, 40, y); y += 30;

        gfx.DrawString("Top Leistungsziffern", headerFont, XBrushes.Black, 40, y);
        y += 20;

        foreach (var item in services.Take(10))
        {
            gfx.DrawString($"{item.Name} - Anzahl: {item.Count} - Betrag: {item.Amount:C}",
                textFont, XBrushes.Black, 40, y);
            y += 16;
        }

        y += 20;
        gfx.DrawString($"Patienten ohne Karte: {patientsWithoutCard.Count}", headerFont, XBrushes.Black, 40, y);

        document.Save(dialog.FileName);

        MessageBox.Show("PDF wurde exportiert.");
    }

    //Excel export
    private async void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        var from = FromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-1);
        var to = ToDatePicker.SelectedDate ?? DateTime.Today;

        var summary = await _reportsService.GetSummaryAsync(from, to);
        var services = await _reportsService.GetServiceCodeStatsAsync(from, to);
        var patientStats = await _reportsService.GetPatientStatsAsync();
        var patientsWithoutCard = await _reportsService.GetPatientsWithoutCardAsync();

        var dialog = new SaveFileDialog
        {
            Filter = "Excel-Datei (*.xlsx)|*.xlsx",
            FileName = $"Auswertung_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        using var workbook = new XLWorkbook();

        var overview = workbook.Worksheets.Add("Übersicht");
        overview.Cell(1, 1).Value = "Kennzahl";
        overview.Cell(1, 2).Value = "Wert";
        overview.Cell(2, 1).Value = "Patienten";
        overview.Cell(2, 2).Value = summary.PatientCount;
        overview.Cell(3, 1).Value = "Termine";
        overview.Cell(3, 2).Value = summary.AppointmentCount;
        overview.Cell(4, 1).Value = "Diagnosen";
        overview.Cell(4, 2).Value = summary.DiagnosisCount;
        overview.Cell(5, 1).Value = "Rechnungen";
        overview.Cell(5, 2).Value = summary.InvoiceCount;
        overview.Cell(6, 1).Value = "Umsatz";
        overview.Cell(6, 2).Value = summary.Revenue;
        overview.Columns().AdjustToContents();

        var serviceSheet = workbook.Worksheets.Add("Leistungsziffern");
        serviceSheet.Cell(1, 1).Value = "Leistung";
        serviceSheet.Cell(1, 2).Value = "Anzahl";
        serviceSheet.Cell(1, 3).Value = "Betrag";

        for (int i = 0; i < services.Count; i++)
        {
            serviceSheet.Cell(i + 2, 1).Value = services[i].Name;
            serviceSheet.Cell(i + 2, 2).Value = services[i].Count;
            serviceSheet.Cell(i + 2, 3).Value = services[i].Amount;
        }

        serviceSheet.Columns().AdjustToContents();

        var patientSheet = workbook.Worksheets.Add("Patienten Statistik");
        patientSheet.Cell(1, 1).Value = "Versicherung";
        patientSheet.Cell(1, 2).Value = "Anzahl";

        for (int i = 0; i < patientStats.Count; i++)
        {
            patientSheet.Cell(i + 2, 1).Value = patientStats[i].Name;
            patientSheet.Cell(i + 2, 2).Value = patientStats[i].Count;
        }

        patientSheet.Columns().AdjustToContents();

        var noCardSheet = workbook.Worksheets.Add("Ohne Karte");
        noCardSheet.Cell(1, 1).Value = "Vorname";
        noCardSheet.Cell(1, 2).Value = "Nachname";
        noCardSheet.Cell(1, 3).Value = "Geburtsdatum";
        noCardSheet.Cell(1, 4).Value = "Telefon";
        noCardSheet.Cell(1, 5).Value = "E-Mail";

        for (int i = 0; i < patientsWithoutCard.Count; i++)
        {
            var p = patientsWithoutCard[i];

            noCardSheet.Cell(i + 2, 1).Value = p.Vorname;
            noCardSheet.Cell(i + 2, 2).Value = p.Nachname;
            noCardSheet.Cell(i + 2, 3).Value = p.Geburtsdatum;
            noCardSheet.Cell(i + 2, 4).Value = p.Telefonnummer;
            noCardSheet.Cell(i + 2, 5).Value = p.Email;
        }

        noCardSheet.Columns().AdjustToContents();

        workbook.SaveAs(dialog.FileName);

        MessageBox.Show("Excel wurde exportiert.");
    }


}