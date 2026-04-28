using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Praxis.Application.Interfaces;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Client.Views;
using Microsoft.Win32;
using System.Linq;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using System.Windows.Media.Animation;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;

namespace Praxis.Client.Views.Pages.Patienten;

public partial class PatientMedicalRecordPage : System.Windows.Controls.UserControl
{
    private readonly IPatientMedicalRecordService _medicalRecordService;
    private readonly ObservableCollection<PatientMedicalRecordEntry> _entries = new();

    private Patient? _currentPatient;
    private PatientMedicalRecordEntry? _selectedEntry;
    private readonly List<PatientMedicalRecordEntry> _allEntries = new();
    private const string AllFilterText = "Alle";

    private bool _showTimeline;

    public PatientMedicalRecordEntry? SelectedTimelineEntry { get; set; }

    public PatientMedicalRecordPage(IPatientMedicalRecordService medicalRecordService)
    {
        InitializeComponent();

        _medicalRecordService = medicalRecordService;

        EntriesGrid.ItemsSource = _entries;
        EntryTypeComboBox.ItemsSource = Enum.GetValues(typeof(MedicalRecordEntryType));
        EntryTypeComboBox.SelectedItem = MedicalRecordEntryType.Notiz;
        var filterItems = new List<object> { AllFilterText };
        filterItems.AddRange(Enum.GetValues(typeof(MedicalRecordEntryType)).Cast<object>());

        FilterTypeComboBox.ItemsSource = filterItems;
        FilterTypeComboBox.SelectedItem = AllFilterText;

        UpdateButtonStates();
    }

    public async Task LoadPatientAsync(Patient patient)
    {
        _currentPatient = patient;

        PatientHeaderTextBlock.Text =
            $"{patient.FullName} · geb. {patient.Geburtsdatum:dd.MM.yyyy} · {patient.Versicherung}";

        ClearForm();

        await LoadEntriesAsync(patient.Id);
    }

    public async Task RefreshAsync()
    {
        if (_currentPatient != null)
            await LoadEntriesAsync(_currentPatient.Id);
    }

    private async Task LoadEntriesAsync(int patientId)
    {
        try
        {
            _entries.Clear();
            _allEntries.Clear();

            var entries = await _medicalRecordService.GetByPatientAsync(patientId);

            foreach (var entry in entries)
                _allEntries.Add(entry);

            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim Laden der Karteikarte:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    private void FilterTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _entries.Clear();

        IEnumerable<PatientMedicalRecordEntry> filtered = _allEntries;

        if (FilterTypeComboBox.SelectedItem is MedicalRecordEntryType selectedType)
        {
            filtered = filtered.Where(x => x.EntryType == selectedType);
        }

        var search = SearchTextBox.Text?.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(x =>
                (x.Title ?? string.Empty).ToLowerInvariant().Contains(search) ||
                (x.Text ?? string.Empty).ToLowerInvariant().Contains(search) ||
                (x.IcdCode ?? string.Empty).ToLowerInvariant().Contains(search) ||
                (x.IcdText ?? string.Empty).ToLowerInvariant().Contains(search) ||
                x.EntryType.ToString().ToLowerInvariant().Contains(search) ||
                (x.Invoice?.InvoiceNumber ?? string.Empty).ToLowerInvariant().Contains(search));
        }

        foreach (var entry in filtered.OrderByDescending(x => x.CreatedAt))
            _entries.Add(entry);

        EntriesGrid.SelectedItem = null;
        ClearForm();
    }
    public async Task LoadPatientAndSelectLaborEntryAsync(Patient patient, int laborRecordId)
    {
        await LoadPatientAsync(patient);

        var entry = _entries.FirstOrDefault(x =>
            x.EntryType == MedicalRecordEntryType.Labor &&
            x.LaborRecordId == laborRecordId);

        if (entry != null)
        {
            EntriesGrid.SelectedItem = entry;
            EntriesGrid.ScrollIntoView(entry);
        }
    }

    private void EntriesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EntriesGrid.SelectedItem is not PatientMedicalRecordEntry entry)
            return;

        _selectedEntry = entry;

        EntryTypeComboBox.SelectedItem = entry.EntryType;
        TitleTextBox.Text = entry.Title;
        TextTextBox.Text = entry.Text;
        IcdCodeTextBox.Text = entry.IcdCode ?? string.Empty;
        IcdTextTextBox.Text = entry.IcdText ?? string.Empty;
        CreatedByTextBox.Text = entry.CreatedBy;
        UpdateButtonStates();

    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        EntriesGrid.SelectedItem = null;
        ClearForm();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPatient == null)
        {
            MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
            return;
        }

        if (EntryTypeComboBox.SelectedItem is not MedicalRecordEntryType entryType)
        {
            MessageBox.Show("Bitte einen Karteikarten-Typ auswählen.");
            return;
        }

        if (string.IsNullOrWhiteSpace(TextTextBox.Text)
            && entryType is not MedicalRecordEntryType.Dokument
            && entryType is not MedicalRecordEntryType.Labor
            && entryType is not MedicalRecordEntryType.Abrechnung)
        {
            MessageBox.Show("Bitte einen Text eingeben.");
            return;
        }

        try
        {
            if (_selectedEntry == null)
            {
                var newEntry = new PatientMedicalRecordEntry
                {
                    PatientId = _currentPatient.Id,
                    EntryType = entryType,
                    Title = string.IsNullOrWhiteSpace(TitleTextBox.Text)
                        ? entryType.ToString()
                        : TitleTextBox.Text.Trim(),
                    Text = TextTextBox.Text.Trim(),
                    IcdCode = NullIfEmpty(IcdCodeTextBox.Text),
                    IcdText = NullIfEmpty(IcdTextTextBox.Text),
                    CreatedBy = CreatedByTextBox.Text.Trim()
                };

                await _medicalRecordService.AddAsync(newEntry);
            }
            else
            {
                _selectedEntry.EntryType = entryType;
                _selectedEntry.Title = string.IsNullOrWhiteSpace(TitleTextBox.Text)
                    ? entryType.ToString()
                    : TitleTextBox.Text.Trim();
                _selectedEntry.Text = TextTextBox.Text.Trim();
                _selectedEntry.IcdCode = NullIfEmpty(IcdCodeTextBox.Text);
                _selectedEntry.IcdText = NullIfEmpty(IcdTextTextBox.Text);
                _selectedEntry.CreatedBy = CreatedByTextBox.Text.Trim();

                await _medicalRecordService.UpdateAsync(_selectedEntry);
            }

            await LoadEntriesAsync(_currentPatient.Id);
            ClearForm();

            MessageBox.Show("Karteikarten-Eintrag gespeichert.");
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

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedEntry == null)
        {
            MessageBox.Show("Bitte zuerst einen Eintrag auswählen.");
            return;
        }

        var result = MessageBox.Show(
            "Diesen Karteikarten-Eintrag wirklich löschen?",
            "Löschen bestätigen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            await _medicalRecordService.DeleteAsync(_selectedEntry.Id);

            if (_currentPatient != null)
                await LoadEntriesAsync(_currentPatient.Id);

            ClearForm();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim Löschen:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void UpdateButtonStates()
    {
        var hasSelection = _selectedEntry != null;

        DeleteButton.IsEnabled = hasSelection;

        OpenLaborRecordButton.IsEnabled =
            hasSelection &&
            _selectedEntry!.EntryType == MedicalRecordEntryType.Labor &&
            _selectedEntry.LaborRecordId.HasValue;

        OpenInvoiceButton.IsEnabled =
            hasSelection &&
            _selectedEntry!.InvoiceId.HasValue;
    }

    private void ClearForm()
    {
        _selectedEntry = null;

        EntryTypeComboBox.SelectedItem = MedicalRecordEntryType.Notiz;
        TitleTextBox.Clear();
        TextTextBox.Clear();
        IcdCodeTextBox.Clear();
        IcdTextTextBox.Clear();
        CreatedByTextBox.Clear();
        UpdateButtonStates();
    }

    private static string? NullIfEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private async void OpenLaborRecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedEntry == null)
        {
            MessageBox.Show("Bitte zuerst einen Karteikarten-Eintrag auswählen.");
            return;
        }

        if (_selectedEntry.EntryType != MedicalRecordEntryType.Labor || _selectedEntry.LaborRecordId == null)
        {
            MessageBox.Show("Dieser Karteikarten-Eintrag ist kein verknüpfter Laborbericht.");
            return;
        }

        if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
            return;

        await mainWindow.OpenLaborRecordPageAsync(_selectedEntry.LaborRecordId.Value);
    }
    private async void OpenInvoiceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedEntry == null)
        {
            MessageBox.Show("Bitte zuerst einen Karteikarten-Eintrag auswählen.");
            return;
        }

        if (_selectedEntry.InvoiceId == null)
        {
            MessageBox.Show("Dieser Karteikarten-Eintrag ist noch nicht abgerechnet.");
            return;
        }

        if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
            return;

        try
        {
            var invoiceService = mainWindow.ServiceProvider.GetRequiredService<IInvoiceService>();
            var pdfService = mainWindow.ServiceProvider.GetRequiredService<IInvoicePdfService>();

            var invoice = await invoiceService.GetInvoiceByIdAsync(_selectedEntry.InvoiceId.Value);

            if (invoice == null)
            {
                MessageBox.Show("Rechnung wurde nicht gefunden.");
                return;
            }

            var result = MessageBox.Show(
                $"Rechnung gefunden:\n\n" +
                $"Nummer: {invoice.InvoiceNumber}\n" +
                $"Datum: {invoice.InvoiceDate:dd.MM.yyyy}\n" +
                $"Betrag: {invoice.TotalAmount:N2} €\n\n" +
                $"Als PDF exportieren?",
                "Rechnung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes)
                return;

            var dialog = new SaveFileDialog
            {
                Title = "Rechnung als PDF speichern",
                Filter = "PDF-Datei (*.pdf)|*.pdf",
                FileName = $"{invoice.InvoiceNumber}.pdf"
            };

            if (dialog.ShowDialog() != true)
                return;

            pdfService.ExportInvoiceToPdf(invoice, dialog.FileName);

            MessageBox.Show("PDF wurde exportiert.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Rechnung konnte nicht geöffnet werden:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }
    // Timeline
    private void ToggleTimelineButton_Click(object sender, RoutedEventArgs e)
    {
        _showTimeline = !_showTimeline;

        if (_showTimeline)
        {
            EntriesGrid.Visibility = Visibility.Collapsed;
            TimelineList.Visibility = Visibility.Visible;
            ToggleTimelineButton.Content = "Tabelle";

            LoadTimeline();
        }
        else
        {
            EntriesGrid.Visibility = Visibility.Visible;
            TimelineList.Visibility = Visibility.Collapsed;
            ToggleTimelineButton.Content = "Timeline";
        }
    }
 
    private void LoadTimeline()
    {
        var grouped = _entries
            .OrderByDescending(x => x.CreatedAt)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new TimelineGroup
            {
                Date = g.Key,
                Entries = g.ToList()
            })
            .ToList();

        TimelineList.ItemsSource = grouped;
    }
    private void TimelineEntry_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not Border border)
            return;

        if (border.DataContext is not PatientMedicalRecordEntry entry)
            return;

        SelectedTimelineEntry = entry;
        _selectedEntry = entry;

        EntryTypeComboBox.SelectedItem = entry.EntryType;
        TitleTextBox.Text = entry.Title;
        TextTextBox.Text = entry.Text;
        IcdCodeTextBox.Text = entry.IcdCode ?? "";
        IcdTextTextBox.Text = entry.IcdText ?? "";
        CreatedByTextBox.Text = entry.CreatedBy;

        UpdateTimelineHighlight();
        UpdateButtonStates();
    }
    private void UpdateTimelineHighlight()
    {
        ResetBorders(TimelineList);
    }

    private void ResetBorders(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is Border border &&
                border.DataContext is PatientMedicalRecordEntry entry)
            {
                if (SelectedTimelineEntry != null && entry.Id == SelectedTimelineEntry.Id)
                {
                    border.Background = Brushes.DodgerBlue;
                    border.BorderBrush = Brushes.RoyalBlue;
                    border.BorderThickness = new Thickness(2);
                }
                else
                {
                    border.ClearValue(Border.BackgroundProperty);
                    border.ClearValue(Border.BorderBrushProperty);
                    border.ClearValue(Border.BorderThicknessProperty);
                }
            }

            ResetBorders(child);
        }
    }

}
public class TimelineGroup
{
    public DateTime Date { get; set; }
    public List<PatientMedicalRecordEntry> Entries { get; set; } = new();
}