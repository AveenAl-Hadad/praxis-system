using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using System.Linq;
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
        
        // Neue
        public async Task ShowImportAsync()
        {
            SetTitle("Labordaten importieren");
            await LoadStoredDataAsync();
        }
        public async Task ShowLaborBooksAsync()
        {
            SetTitle("Laborbücher zuordnen");
            LaborGrid.ItemsSource = await _laborService.GetAllAsync();
            UpdateStatusInfo(0, 0, "Laborbücher / Zuordnung geladen");
        }
        public async Task ShowAssignedReportsAsync()
        {
            SetTitle("Zugeordnete Laborberichte");
            var records = await _laborService.GetAllAsync();
            LaborGrid.ItemsSource = records
                .Where(x => x.Status == "Importiert" || x.Status == "Zugeordnet")
                .ToList();

            UpdateStatusInfo(0, 0, "Zugeordnete Laborberichte geladen");
        }
        public async Task ShowDailyListAsync()
        {
            SetTitle("Labortagesliste");

            var today = DateTime.Today.ToString("dd.MM.yyyy");
            var records = await _laborService.GetAllAsync();

            LaborGrid.ItemsSource = records
                .Where(x => !string.IsNullOrWhiteSpace(x.Erstellt) && x.Erstellt.StartsWith(today))
                .ToList();

            UpdateStatusInfo(0, 0, $"Labortagesliste für {today} geladen");
        }
        public async Task ShowLabsAsync()
        {
            SetTitle("Labore");

            var records = await _laborService.GetAllAsync();

            LaborGrid.ItemsSource = records
                .GroupBy(x => x.Labor)
                .Select(g => new LaborRecord
                {
                    Datei = $"{g.Count()} Bericht(e)",
                    Labor = g.Key,
                    Erstellt = "-",
                    Betriebsstaette = "-",
                    Bsnr = "-",
                    Kundennummer = "-",
                    Status = "Laborübersicht"
                })
                .ToList();

            UpdateStatusInfo(0, 0, "Laborübersicht geladen");
        }
        private void SetTitle(string title)
        {
            PageTitleTextBlock.Text = title;
        }
        private async void AssignPatient_Click(object sender, RoutedEventArgs e)
        {
            if (LaborGrid.SelectedItem is not LaborRecord record)
            {
                MessageBox.Show("Bitte einen Datensatz auswählen.");
                return;
            }

            if (System.Windows.Application.Current.MainWindow is not MainWindow main)
                return;

            var patients = (await main.GetPatientsAsync()).ToList();

            var patient = patients.FirstOrDefault(); // später Dialog

            if (patient == null)
            {
                MessageBox.Show("Kein Patient gefunden.");
                return;
            }

            await _laborService.AssignToPatientAsync(record.Id, patient.Id);

            await ShowLaborBooksAsync();
        }
        private async void MarkError_Click(object sender, RoutedEventArgs e)
        {
            if (LaborGrid.SelectedItem is not LaborRecord record)
            {
                MessageBox.Show("Bitte einen Datensatz auswählen.");
                return;
            }

            await _laborService.SetStatusAsync(record.Id, "Fehler");

            await ShowLaborBooksAsync();
        }
    }
}


