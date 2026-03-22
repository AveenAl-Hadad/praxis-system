using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Client.Session;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

/// <summary>
/// Dieses Fenster zeigt alle Rechnungen und ermöglicht deren Verwaltung.
/// 
/// Funktionen:
/// - Rechnungen anzeigen
/// - nach Patient und Datum filtern
/// - neue Rechnungen erstellen
/// - Rechnungen löschen
/// - Rechnungen als PDF exportieren
/// 
/// Zusätzlich wird geprüft, ob der Benutzer die nötigen Berechtigungen hat.
/// </summary>
public partial class InvoiceWindow : Window
{
    /// <summary>
    /// Service für Rechnungsoperationen (laden, speichern, löschen).
    /// </summary>
    private readonly IInvoiceService _invoiceService;

    /// <summary>
    /// ServiceProvider für Dependency Injection (z. B. Fenster erstellen).
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Service zum Exportieren von Rechnungen als PDF.
    /// </summary>
    private readonly IInvoicePdfService _invoicePdfService;

    /// <summary>
    /// Liste aller geladenen Rechnungen (für Filterung).
    /// </summary>
    private List<Invoice> _allInvoices = new();

    /// <summary>
    /// Konstruktor des Fensters.
    /// Initialisiert die Services und lädt beim Öffnen die Rechnungen.
    /// </summary>
    public InvoiceWindow(IInvoiceService invoiceService, IServiceProvider serviceProvider, IInvoicePdfService invoicePdfService)
    {
        InitializeComponent();
        _invoiceService = invoiceService;
        _serviceProvider = serviceProvider;
        _invoicePdfService = invoicePdfService;

        Loaded += InvoiceWindow_Loaded;
    }

    /// <summary>
    /// Wird beim Laden des Fensters ausgeführt.
    /// 
    /// - prüft die Benutzerrechte
    /// - lädt alle Rechnungen
    /// </summary>
    private async void InvoiceWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CheckAccess();
        await LoadInvoicesAsync();
    }

    /// <summary>
    /// Lädt alle Rechnungen aus der Datenbank
    /// und wendet anschließend die Filter an.
    /// </summary>
    private async Task LoadInvoicesAsync()
    {
        _allInvoices = await _invoiceService.GetAllInvoicesAsync();
        ApplyFilter();
    }

    /// <summary>
    /// Filtert die Rechnungen nach:
    /// - Patientenname
    /// - Von-Datum
    /// - Bis-Datum
    /// </summary>
    private void ApplyFilter()
    {
        IEnumerable<Invoice> query = _allInvoices;

        // Textfilter nach Patient
        var patientText = PatientFilterBox.Text?.Trim().ToLower() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(patientText))
        {
            query = query.Where(i =>
                i.Patient != null &&
                i.Patient.FullName.ToLower().Contains(patientText));
        }

        // Filter: Von-Datum
        if (FromDatePicker.SelectedDate.HasValue)
        {
            var from = FromDatePicker.SelectedDate.Value.Date;
            query = query.Where(i => i.InvoiceDate.Date >= from);
        }

        // Filter: Bis-Datum
        if (ToDatePicker.SelectedDate.HasValue)
        {
            var to = ToDatePicker.SelectedDate.Value.Date;
            query = query.Where(i => i.InvoiceDate.Date <= to);
        }

        // Gefilterte Liste anzeigen
        InvoicesGrid.ItemsSource = query.ToList();
    }

    /// <summary>
    /// Wird ausgelöst, wenn sich ein Filterwert ändert.
    /// Aktualisiert die Anzeige.
    /// </summary>
    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        ApplyFilter();
    }

    /// <summary>
    /// Öffnet das Fenster zum Erstellen einer neuen Rechnung.
    /// </summary>
    private void NewInvoice_Click(object sender, RoutedEventArgs e)
    {
        var window = (AddInvoiceWindow)_serviceProvider.GetRequiredService(typeof(AddInvoiceWindow));
        window.Owner = this;

        if (window.ShowDialog() == true)
        {
            _ = SaveNewInvoiceAsync(window.CreatedInvoice);
        }
    }

    /// <summary>
    /// Speichert eine neu erstellte Rechnung in der Datenbank
    /// und aktualisiert die Anzeige.
    /// </summary>
    private async Task SaveNewInvoiceAsync(Invoice? invoice)
    {
        if (invoice == null) return;

        await _invoiceService.AddInvoiceAsync(invoice, UserSession.CurrentUser?.Username ?? "System");

        // Liste und Dashboard aktualisieren
        await LoadInvoicesAsync();
        await ((MainWindow)Application.Current.MainWindow).LoadDashboardAsync();
    }

    /// <summary>
    /// Exportiert die ausgewählte Rechnung als PDF-Datei.
    /// </summary>
    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (InvoicesGrid.SelectedItem is not Invoice invoice)
        {
            MessageBox.Show("Bitte zuerst eine Rechnung auswählen.");
            return;
        }

        // Dialog zum Speichern der PDF-Datei
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

    /// <summary>
    /// Prüft, ob der Benutzer die nötigen Rechte hat,
    /// um Rechnungen zu sehen.
    /// </summary>
    private void CheckAccess()
    {
        if (!UserSession.HasAnyRole(Roles.Administrator, Roles.Mitarbeiter))
        {
            MessageBox.Show("Sie haben keine Berechtigung für Rechnungen.");
            Close();
        }
    }

    /// <summary>
    /// Löscht die ausgewählte Rechnung nach Bestätigung.
    /// </summary>
    private async void DeleteInvoice_Click(object sender, RoutedEventArgs e)
    {
        if (InvoicesGrid.SelectedItem is not Invoice invoice)
        {
            MessageBox.Show("Bitte zuerst eine Rechnung auswählen.");
            return;
        }

        // Sicherheitsabfrage
        var result = MessageBox.Show(
            $"Möchten Sie die Rechnung '{invoice.InvoiceNumber}' wirklich löschen?",
            "Rechnung löschen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            await _invoiceService.DeleteInvoiceAsync(
                invoice.Id,
                UserSession.CurrentUser?.Username ?? "System");

            // Liste und Dashboard aktualisieren
            await LoadInvoicesAsync();
            await ((MainWindow)Application.Current.MainWindow).LoadDashboardAsync();

            MessageBox.Show("Rechnung wurde erfolgreich gelöscht.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Löschen der Rechnung:\n{ex.Message}");
        }
    }
}