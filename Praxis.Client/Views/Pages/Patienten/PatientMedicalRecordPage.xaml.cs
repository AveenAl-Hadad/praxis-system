using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Praxis.Application.Interfaces;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Client.Views;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views.Pages.Patienten;

public partial class PatientMedicalRecordPage : System.Windows.Controls.UserControl
{
    private readonly IPatientMedicalRecordService _medicalRecordService;
    private readonly ObservableCollection<PatientMedicalRecordEntry> _entries = new();

    private Patient? _currentPatient;
    private PatientMedicalRecordEntry? _selectedEntry;

    public PatientMedicalRecordPage(IPatientMedicalRecordService medicalRecordService)
    {
        InitializeComponent();

        _medicalRecordService = medicalRecordService;

        EntriesGrid.ItemsSource = _entries;
        EntryTypeComboBox.ItemsSource = Enum.GetValues(typeof(MedicalRecordEntryType));
        EntryTypeComboBox.SelectedItem = MedicalRecordEntryType.Notiz;
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

            var entries = await _medicalRecordService.GetByPatientAsync(patientId);

            foreach (var entry in entries)
                _entries.Add(entry);
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

    private void ClearForm()
    {
        _selectedEntry = null;

        EntryTypeComboBox.SelectedItem = MedicalRecordEntryType.Notiz;
        TitleTextBox.Clear();
        TextTextBox.Clear();
        IcdCodeTextBox.Clear();
        IcdTextTextBox.Clear();
        CreatedByTextBox.Clear();
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

        try
        {
            var laborService = mainWindow.ServiceProvider.GetRequiredService<ILaborService>();
            var labor = await laborService.GetByIdAsync(_selectedEntry.LaborRecordId.Value);

            if (labor == null)
            {
                MessageBox.Show("Der Laborbericht wurde nicht gefunden.");
                return;
            }

            MessageBox.Show(
                $"Laborbericht\n\n" +
                $"Labor: {labor.Labor}\n" +
                $"Datei: {labor.Datei}\n" +
                $"Erstellt: {labor.Erstellt}\n" +
                $"Betriebsstätte: {labor.Betriebsstaette}\n" +
                $"BSNR/BSID: {labor.Bsnr}\n" +
                $"Kundennummer: {labor.Kundennummer}\n" +
                $"Status: {labor.Status}",
                "Laborbericht",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Laborbericht konnte nicht geöffnet werden:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}