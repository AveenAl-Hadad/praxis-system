using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Praxis.Client.Views.Pages.Abrechnung
{
    public partial class AbrechnungPage : System.Windows.Controls.UserControl
    {
        private readonly IAbrechnungService _abrechnungService;
        private Abrechnungsbeleg? _editingItem;
        private bool _isNewMode = false;

        public AbrechnungPage(IAbrechnungService abrechnungService)
        {
            InitializeComponent();
            _abrechnungService = abrechnungService;
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            AbrechnungGrid.ItemsSource = await _abrechnungService.GetAllAsync();
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            _editingItem = null;
            _isNewMode = true;

            TypTextBox.Text = "KV";
            ZeitraumTextBox.Text = "";
            FaelleTextBox.Text = "";
            BetragTextBox.Text = "";
            StatusTextBox.Text = "Neu";
            AktionTextBox.Text = "Erstellt";

            EditorTitleTextBlock.Text = "Neue Abrechnung";
            EditorBorder.Visibility = Visibility.Visible;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (AbrechnungGrid.SelectedItem is not Abrechnungsbeleg selected)
            {
                System.Windows.MessageBox.Show(
                    "Bitte zuerst eine Abrechnung auswählen.",
                    "Hinweis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            _editingItem = selected;
            _isNewMode = false;

            TypTextBox.Text = selected.Typ;
            ZeitraumTextBox.Text = selected.Zeitraum;
            FaelleTextBox.Text = selected.Faelle.ToString();
            BetragTextBox.Text = selected.Betrag.ToString();
            StatusTextBox.Text = selected.Status;
            AktionTextBox.Text = selected.Aktion;

            EditorTitleTextBlock.Text = "Abrechnung bearbeiten";
            EditorBorder.Visibility = Visibility.Visible;
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (AbrechnungGrid.SelectedItem is not Abrechnungsbeleg selected)
            {
                System.Windows.MessageBox.Show(
                    "Bitte zuerst eine Abrechnung auswählen.",
                    "Hinweis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var result = System.Windows.MessageBox.Show(
                "Möchtest du diesen Eintrag wirklich löschen?",
                "Löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            await _abrechnungService.DeleteAsync(selected.Id);
            await LoadDataAsync();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TypTextBox.Text))
                {
                    System.Windows.MessageBox.Show("Bitte Typ eingeben.");
                    return;
                }

                if (!int.TryParse(FaelleTextBox.Text, out int faelle))
                {
                    System.Windows.MessageBox.Show("Anzahl Fälle ist ungültig.");
                    return;
                }

                if (!decimal.TryParse(BetragTextBox.Text, out decimal betrag))
                {
                    System.Windows.MessageBox.Show("Betrag ist ungültig.");
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
                await LoadDataAsync();

                System.Windows.MessageBox.Show(
                    "Abrechnung wurde gespeichert.",
                    "Erfolg",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Fehler beim Speichern: {ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
    }
}