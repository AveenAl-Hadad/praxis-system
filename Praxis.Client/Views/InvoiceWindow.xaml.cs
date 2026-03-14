using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

public partial class InvoiceWindow : Window
{
    private readonly IInvoiceService _invoiceService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IInvoicePdfService _invoicePdfService;
    private List<Invoice> _allInvoices = new();

    public InvoiceWindow(IInvoiceService invoiceService, IServiceProvider serviceProvider, IInvoicePdfService invoicePdfService)
    {
        InitializeComponent();
        _invoiceService = invoiceService;
        _serviceProvider = serviceProvider;
        Loaded += InvoiceWindow_Loaded;
        _invoicePdfService = invoicePdfService;
    }

    private async void InvoiceWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadInvoicesAsync();
    }

    private async Task LoadInvoicesAsync()
    {
        _allInvoices = await _invoiceService.GetAllInvoicesAsync();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<Invoice> query = _allInvoices;

        var patientText = PatientFilterBox.Text?.Trim().ToLower() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(patientText))
        {
            query = query.Where(i =>
                i.Patient != null &&
                i.Patient.FullName.ToLower().Contains(patientText));
        }

        if (FromDatePicker.SelectedDate.HasValue)
        {
            var from = FromDatePicker.SelectedDate.Value.Date;
            query = query.Where(i => i.InvoiceDate.Date >= from);
        }

        if (ToDatePicker.SelectedDate.HasValue)
        {
            var to = ToDatePicker.SelectedDate.Value.Date;
            query = query.Where(i => i.InvoiceDate.Date <= to);
        }

        InvoicesGrid.ItemsSource = query.ToList();
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        ApplyFilter();
    }

    private void NewInvoice_Click(object sender, RoutedEventArgs e)
    {
        var window = (AddInvoiceWindow)_serviceProvider.GetRequiredService(typeof(AddInvoiceWindow));
        window.Owner = this;

        if (window.ShowDialog() == true)
        {
            _ = SaveNewInvoiceAsync(window.CreatedInvoice);
        }
    }

    private async Task SaveNewInvoiceAsync(Invoice? invoice)
    {
        if (invoice == null) return;

        await _invoiceService.AddInvoiceAsync(invoice);
        await LoadInvoicesAsync();
    }
    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (InvoicesGrid.SelectedItem is not Invoice invoice)
        {
            MessageBox.Show("Bitte zuerst eine Rechnung auswählen.");
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PDF Datei (*.pdf)|*.pdf",
            FileName = $"{invoice.InvoiceNumber}.pdf"
        };

        if (dialog.ShowDialog() == true)
        {
            _invoicePdfService.ExportInvoiceToPdf(invoice, dialog.FileName);
            MessageBox.Show("PDF wurde erfolgreich exportiert.");
        }
    }
}