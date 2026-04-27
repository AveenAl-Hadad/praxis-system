using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using System;
using Microsoft.Win32;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Praxis.Client.Views;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Microsoft.Extensions.DependencyInjection;

namespace Praxis.Client.Views.Pages.Abrechnung
{
    public partial class AbrechnungPage : System.Windows.Controls.UserControl
    {
        private readonly IAbrechnungService _abrechnungService;
        private readonly IInvoiceService _invoiceService;
        private readonly IInvoicePdfService _invoicePdfService;
        private readonly IBillingGenerationService _billingGenerationService;
        private Abrechnungsbeleg? _editingItem;
        private bool _isNewMode = false;
        private string _currentView = "Alle";
        private string _currentFilter = "Alle";
       
        private bool _isNew = true;

        public AbrechnungPage(
                                 IAbrechnungService abrechnungService,
                                 IInvoiceService invoiceService,
                                 IInvoicePdfService invoicePdfService,
                                 IBillingGenerationService billingGenerationService)
        {
            InitializeComponent();

            _abrechnungService = abrechnungService;
            _invoiceService = invoiceService;
            _invoicePdfService = invoicePdfService;
            _billingGenerationService = billingGenerationService;

            _ = ShowOverviewAsync();
        }

        public async Task ShowOverviewAsync()
        {
            _currentView = "Alle";
            PageTitleTextBlock.Text = "Abrechnung";
            await LoadDataAsync();
        }

        public async Task ShowNewKvAsync()
        {
            StartNewAbrechnung("KV");
            await LoadDataAsync();
        }

        public async Task ShowKvListAsync()
        {
            _currentView = "KV";
            PageTitleTextBlock.Text = "KV-Abrechnungen";
            EditorBorder.Visibility = Visibility.Collapsed;
            await LoadFilteredAsync("KV");
        }

        public async Task ShowNewPrivateAsync()
        {
            StartNewAbrechnung("Privat");
            await LoadDataAsync();

        }
        public async Task ShowInvoicesAsync()
        {
            _currentView = "Rechnung";
            PageTitleTextBlock.Text = "Rechnungen";
            await LoadDataAsync();
        }

        public async Task ShowRemindersAsync()
        {
            _currentView = "Mahnung";
            PageTitleTextBlock.Text = "Mahnungen";
            await LoadDataAsync();
        }

        public async Task ShowAllAsync()
        {
            _currentFilter = "Alle";
            PageTitleTextBlock.Text = "Abrechnung";
            await LoadDataAsync();
        }

        public async Task ShowKvAsync()
        {
            _currentFilter = "KV";
            PageTitleTextBlock.Text = "KV-Abrechnungen";
            await LoadDataAsync();
        }

        public async Task ShowPrivateAsync()
        {
            _currentFilter = "Privat";
            PageTitleTextBlock.Text = "Privatabrechnungen";
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var items = await _abrechnungService.GetAllAsync();

            if (_currentFilter != "Alle")
            {
                items = items
                    .Where(x => x.Typ == _currentFilter)
                    .ToList();
            }

            AbrechnungGrid.ItemsSource = items
                .OrderByDescending(x => x.Id)
                .ToList();
        }

        private async Task LoadFilteredAsync(string typ)
        {
            var items = await _abrechnungService.GetAllAsync();

            AbrechnungGrid.ItemsSource = items
                .Where(x => string.Equals(x.Typ, typ, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Id)
                .ToList();
        }

        private void OpenNewEditor(string typ)
        {
            _editingItem = null;
            _isNewMode = true;

            TypTextBox.Text = typ;
            ZeitraumTextBox.Text = GetDefaultZeitraum(typ);
            FaelleTextBox.Text = "0";
            BetragTextBox.Text = "0,00";
            StatusTextBox.Text = "Neu";
            AktionTextBox.Text = "Erstellt";

            EditorTitleTextBlock.Text = typ == "Privat"
                ? "Neue Privatabrechnung"
                : typ == "KV"
                    ? "Neue KV-Abrechnung"
                    : "Neue Abrechnung";

            EditorBorder.Visibility = Visibility.Visible;
        }

        private static string GetDefaultZeitraum(string typ)
        {
            var today = DateTime.Today;

            if (typ == "KV")
            {
                var quarter = ((today.Month - 1) / 3) + 1;
                return $"{today.Year}-Q{quarter}";
            }

            return today.ToString("MM/yyyy");
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            OpenNewEditor(_currentView == "Alle" ? "KV" : _currentView);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (AbrechnungGrid.SelectedItem is not Abrechnungsbeleg selected)
            {
                MessageBox.Show("Bitte zuerst eine Abrechnung auswählen.");
                return;
            }

            _editingItem = selected;
            _isNewMode = false;

            TypTextBox.Text = selected.Typ;
            ZeitraumTextBox.Text = selected.Zeitraum;
            FaelleTextBox.Text = selected.Faelle.ToString();
            BetragTextBox.Text = selected.Betrag.ToString("N2", CultureInfo.CurrentCulture);
            StatusTextBox.Text = selected.Status;
            AktionTextBox.Text = selected.Aktion;

            EditorTitleTextBlock.Text = "Abrechnung bearbeiten";
            EditorBorder.Visibility = Visibility.Visible;
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (AbrechnungGrid.SelectedItem is not Abrechnungsbeleg selected)
            {
                MessageBox.Show("Bitte zuerst eine Abrechnung auswählen.");
                return;
            }

            var result = MessageBox.Show(
                "Möchtest du diesen Eintrag wirklich löschen?",
                "Löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            await _abrechnungService.DeleteAsync(selected.Id);
            await ReloadCurrentViewAsync();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TypTextBox.Text))
                {
                    MessageBox.Show("Bitte Typ eingeben.");
                    return;
                }

                if (!int.TryParse(FaelleTextBox.Text, out int faelle))
                {
                    MessageBox.Show("Anzahl Fälle ist ungültig.");
                    return;
                }

                if (!decimal.TryParse(BetragTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal betrag))
                {
                    MessageBox.Show("Betrag ist ungültig.");
                    return;
                }

                if (_isNewMode)
                {
                    var newItem = new Abrechnungsbeleg
                    {
                        Typ = TypTextBox.Text.Trim(),
                        Zeitraum = ZeitraumTextBox.Text.Trim(),
                        Faelle = faelle,
                        Betrag = betrag,
                        Status = StatusTextBox.Text.Trim(),
                        Aktion = AktionTextBox.Text.Trim()
                    };

                    await _abrechnungService.AddAsync(newItem);
                }
                else if (_editingItem != null)
                {
                    _editingItem.Typ = TypTextBox.Text.Trim();
                    _editingItem.Zeitraum = ZeitraumTextBox.Text.Trim();
                    _editingItem.Faelle = faelle;
                    _editingItem.Betrag = betrag;
                    _editingItem.Status = StatusTextBox.Text.Trim();
                    _editingItem.Aktion = AktionTextBox.Text.Trim();

                    await _abrechnungService.UpdateAsync(_editingItem);
                }

                ClearEditor();
                EditorBorder.Visibility = Visibility.Collapsed;
                await ReloadCurrentViewAsync();

                MessageBox.Show("Abrechnung wurde gespeichert.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern:\n{ex.Message}");
            }
        }

        private async Task ReloadCurrentViewAsync()
        {
            switch (_currentView)
            {
                case "KV":
                    await LoadFilteredAsync("KV");
                    break;
                case "Privat":
                    await LoadFilteredAsync("Privat");
                    break;
                case "Rechnung":
                    await LoadFilteredAsync("Rechnung");
                    break;
                case "Mahnung":
                    await LoadFilteredAsync("Mahnung");
                    break;
                default:
                    await LoadDataAsync();
                    break;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ClearEditor();
            EditorBorder.Visibility = Visibility.Collapsed;
        }

        private void ClearEditor()
        {
            _editingItem = null;
            _isNewMode = false;

            TypTextBox.Text = "";
            ZeitraumTextBox.Text = "";
            FaelleTextBox.Text = "";
            BetragTextBox.Text = "";
            StatusTextBox.Text = "";
            AktionTextBox.Text = "";
        }

        private async void MarkOpenButton_Click(object sender, RoutedEventArgs e)
        {
            await ChangeStatusAsync("Offen", "Als offen markiert");
        }

        private async void MarkPaidButton_Click(object sender, RoutedEventArgs e)
        {
            await ChangeStatusAsync("Bezahlt", "Als bezahlt markiert");
        }

        private async void MarkReminderButton_Click(object sender, RoutedEventArgs e)
        {
            await ChangeTypeAndStatusAsync("Mahnung", "Offen", "Mahnung erstellt");
        }

        private async Task ChangeStatusAsync(string status, string action)
        {
            if (AbrechnungGrid.SelectedItem is not Abrechnungsbeleg selected)
            {
                MessageBox.Show("Bitte zuerst einen Eintrag auswählen.");
                return;
            }

            selected.Status = status;
            selected.Aktion = action;

            await _abrechnungService.UpdateAsync(selected);
            await ReloadCurrentViewAsync();
        }

        private async Task ChangeTypeAndStatusAsync(string typ, string status, string action)
        {
            if (AbrechnungGrid.SelectedItem is not Abrechnungsbeleg selected)
            {
                MessageBox.Show("Bitte zuerst einen Eintrag auswählen.");
                return;
            }

            selected.Typ = typ;
            selected.Status = status;
            selected.Aktion = action;

            await _abrechnungService.UpdateAsync(selected);
            await LoadDataAsync();
        }

        //Neue KV-Abrechnung“ und „Neue Privatabrechnung
        private void StartNewAbrechnung(string typ)
        {
            _currentFilter = typ;
            _editingItem = null;
            _isNew = true;

            PageTitleTextBlock.Text = typ == "KV"
                ? "Neue KV-Abrechnung"
                : "Neue Privatabrechnung";

            TypTextBox.Text = typ;
            ZeitraumTextBox.Text = typ == "KV"
                ? GetCurrentQuarter()
                : DateTime.Today.ToString("MM/yyyy");

            FaelleTextBox.Text = "0";
            BetragTextBox.Text = "0,00";
            StatusTextBox.Text = "Offen";
            AktionTextBox.Text = "Neu erstellt";

            AbrechnungGrid.SelectedItem = null;
        }
        //Hilfsmethode ergänzen
        private static string GetCurrentQuarter()
        {
            var today = DateTime.Today;
            var quarter = ((today.Month - 1) / 3) + 1;

            return $"{today.Year}-Q{quarter}";
        }

        //PDF-Export Methode einfügen
        private async void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (AbrechnungGrid.SelectedItem is not Abrechnungsbeleg beleg)
            {
                MessageBox.Show("Bitte zuerst eine Abrechnung auswählen.");
                return;
            }

            var invoices = await _invoiceService.GetAllInvoicesAsync();

            var invoice = invoices
                .OrderByDescending(x => x.InvoiceDate)
                .FirstOrDefault(x =>
                    x.InvoiceNumber == beleg.Aktion ||
                    x.InvoiceNumber.Contains(beleg.Zeitraum) ||
                    x.Patient != null);

            if (invoice == null)
            {
                MessageBox.Show(
                    "Zu diesem Abrechnungseintrag wurde keine technische Rechnung gefunden.\n\nErstelle zuerst eine Rechnung aus einem Termin.",
                    "Keine Rechnung",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Rechnung als PDF speichern",
                Filter = "PDF-Datei (*.pdf)|*.pdf",
                FileName = $"{invoice.InvoiceNumber}.pdf"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                _invoicePdfService.ExportInvoiceToPdf(invoice, dialog.FileName);

                MessageBox.Show(
                    "PDF wurde exportiert.",
                    "PDF",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"PDF konnte nicht erstellt werden:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        //Rechnung aus Termin erstellen
        private async void CreateFromMedicalRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            try
            {
                var dialog = mainWindow.ServiceProvider.GetRequiredService<CreateInvoiceFromMedicalRecordWindow>();
                dialog.Owner = mainWindow;

                if (dialog.ShowDialog() != true ||
                    dialog.SelectedPatient == null ||
                    dialog.SelectedEntryIds.Count == 0)
                {
                    return;
                }

                var invoice = await _billingGenerationService
                    .CreateInvoiceFromMedicalRecordEntriesAsync(
                        dialog.SelectedPatient.Id,
                        dialog.SelectedEntryIds);

                var beleg = new Abrechnungsbeleg
                {
                    Typ = "Rechnung",
                    Zeitraum = invoice.InvoiceDate.ToString("MM/yyyy"),
                    Faelle = 1,
                    Betrag = invoice.TotalAmount,
                    Status = "Offen",
                    Aktion = invoice.InvoiceNumber
                };

                await _abrechnungService.AddAsync(beleg);

                _currentFilter = "Rechnung";
                PageTitleTextBlock.Text = "Rechnungen";

                await LoadDataAsync();

                MessageBox.Show(
                    $"Rechnung wurde aus Karteikarte erstellt:\n{invoice.InvoiceNumber}",
                    "Rechnung erstellt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Rechnung konnte nicht erstellt werden:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


    }
}