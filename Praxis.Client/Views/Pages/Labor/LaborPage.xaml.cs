using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using Praxis.Infrastructure.Services.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views.Pages.Labor
{
    public partial class LaborPage : System.Windows.Controls.UserControl
    {
        private readonly ILaborService _laborService;
        private readonly List<LaborRecord> _previewItems = new();

        public LaborPage(ILaborService laborService)
        {
            InitializeComponent();
            _laborService = laborService;
            _ = LoadStoredDataAsync();
        }
        private async Task LoadStoredDataAsync()
        {
            LaborGrid.ItemsSource = await _laborService.GetAllAsync();
            UpdateStatusInfo(0, 0, "Gespeicherte Datensätze geladen");
        }

        private void UpdateStatusInfo(int waitingCount, int errorCount, string info)
        {
            WaitingCountTextBlock.Text = $"{waitingCount} Dateien in Vorschau";
            ErrorCountTextBlock.Text = $"{errorCount} Dateien mit Fehler";
            LastCheckTextBlock.Text = info;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Labor-Import-Verzeichnis auswählen";

            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                DirectoryTextBox.Text = dialog.SelectedPath;
            }
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            LoadPreviewFromDirectory();
        }

        private void LoadPreviewFromDirectory()
        {
            try
            {
                var path = DirectoryTextBox.Text?.Trim();

                if (string.IsNullOrWhiteSpace(path))
                {
                    MessageBox.Show("Bitte zuerst ein Verzeichnis eingeben.");
                    return;
                }

                if (!Directory.Exists(path))
                {
                    MessageBox.Show("Das Verzeichnis wurde nicht gefunden.");
                    return;
                }

                var files = Directory.GetFiles(path, "*.ldt");
                _previewItems.Clear();

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);

                    _previewItems.Add(new LaborRecord
                    {
                        Datei = Path.GetFileName(file),
                        Labor = "Vorschau",
                        Erstellt = fileInfo.CreationTime.ToString("dd.MM.yyyy HH:mm"),
                        Betriebsstaette = "Unbekannt",
                        Bsnr = "-",
                        Kundennummer = "-",
                        Status = "Bereit zur Übernahme"
                    });
                }

                LaborGrid.ItemsSource = null;
                LaborGrid.ItemsSource = _previewItems;

                UpdateStatusInfo(
                    _previewItems.Count,
                    0,
                    $"Vorschau geladen: {DateTime.Now:dd.MM.yyyy HH:mm}");

                if (_previewItems.Count == 0)
                {
                    MessageBox.Show("Keine .ldt-Dateien gefunden.");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusInfo(0, 1, "Fehler beim Laden der Vorschau");
                MessageBox.Show(
                    $"Fehler beim Laden der Vorschau:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void SaveImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_previewItems.Count == 0)
                {
                    MessageBox.Show("Keine Vorschau-Daten zum Speichern vorhanden.");
                    return;
                }

                foreach (var item in _previewItems)
                {
                    var record = new LaborRecord
                    {
                        Datei = item.Datei,
                        Labor = item.Labor,
                        Erstellt = item.Erstellt,
                        Betriebsstaette = item.Betriebsstaette,
                        Bsnr = item.Bsnr,
                        Kundennummer = item.Kundennummer,
                        Status = "Importiert"
                    };

                    await _laborService.AddAsync(record);
                }

                MessageBox.Show(
                    "Import wurde erfolgreich gespeichert.",
                    "Erfolg",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _previewItems.Clear();
                await LoadStoredDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Speichern:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void RefreshStoredButton_Click(object sender, RoutedEventArgs e)
        {
            _previewItems.Clear();
            await LoadStoredDataAsync();
        }

        private void ClearDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            _previewItems.Clear();
            DirectoryTextBox.Text = string.Empty;
            LaborGrid.ItemsSource = null;
            UpdateStatusInfo(0, 0, "Vorschau zurückgesetzt");
        }
    }
}


