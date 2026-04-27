using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views.Pages.Abrechnung
{
    public partial class AbrechnungPage : System.Windows.Controls.UserControl
    {
        private readonly IAbrechnungService _abrechnungService;
        private Abrechnungsbeleg? _editingItem;
        private bool _isNewMode = false;
        private string _currentView = "Alle";

        public AbrechnungPage(IAbrechnungService abrechnungService)
        {
            InitializeComponent();
            _abrechnungService = abrechnungService;
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
            _currentView = "KV";
            PageTitleTextBlock.Text = "Neue KV-Abrechnung";
            await LoadFilteredAsync("KV");
            OpenNewEditor("KV");
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
            _currentView = "Privat";
            PageTitleTextBlock.Text = "Neue Privatabrechnung";
            await LoadFilteredAsync("Privat");
            OpenNewEditor("Privat");
        }

        public async Task ShowInvoicesAsync()
        {
            _currentView = "Rechnung";
            PageTitleTextBlock.Text = "Rechnungen";
            EditorBorder.Visibility = Visibility.Collapsed;
            await LoadFilteredAsync("Rechnung");
        }

        public async Task ShowRemindersAsync()
        {
            _currentView = "Mahnung";
            PageTitleTextBlock.Text = "Mahnungen";
            EditorBorder.Visibility = Visibility.Collapsed;
            await LoadFilteredAsync("Mahnung");
        }

        private async Task LoadDataAsync()
        {
            var items = await _abrechnungService.GetAllAsync();
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
       
    }
}