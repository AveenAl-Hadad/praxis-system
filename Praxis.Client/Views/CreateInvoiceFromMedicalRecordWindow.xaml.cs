using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Praxis.Application.Interfaces;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views;

public partial class CreateInvoiceFromMedicalRecordWindow : Window
{
    private readonly IPatientService _patientService;
    private readonly IPatientMedicalRecordService _medicalRecordService;

    private readonly ObservableCollection<BillableMedicalRecordEntryRow> _rows = new();

    public Patient? SelectedPatient { get; private set; }
    public List<int> SelectedEntryIds { get; private set; } = new();

    public CreateInvoiceFromMedicalRecordWindow(
        IPatientService patientService,
        IPatientMedicalRecordService medicalRecordService)
    {
        InitializeComponent();

        _patientService = patientService;
        _medicalRecordService = medicalRecordService;

        EntriesGrid.ItemsSource = _rows;

        Loaded += CreateInvoiceFromMedicalRecordWindow_Loaded;
    }

    private async void CreateInvoiceFromMedicalRecordWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var patients = await _patientService.GetAllPatientsAsync();

        PatientComboBox.ItemsSource = patients
            .Where(x => x.IsActive)
            .OrderBy(x => x.Nachname)
            .ThenBy(x => x.Vorname)
            .ToList();
    }

    private async void PatientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PatientComboBox.SelectedItem is not Patient patient)
            return;

        SelectedPatient = patient;
        await LoadEntriesAsync(patient.Id);
    }

    private async Task LoadEntriesAsync(int patientId)
    {
        _rows.Clear();

        var entries = await _medicalRecordService.GetByPatientAsync(patientId);

        foreach (var entry in entries
                     .Where(x => x.CatalogItemId != null && x.CatalogItem != null)
                     .OrderByDescending(x => x.CreatedAt))
        {
            _rows.Add(new BillableMedicalRecordEntryRow
            {
                Id = entry.Id,
                CreatedAt = entry.CreatedAt,
                EntryType = entry.EntryType,
                Title = entry.Title,
                CatalogItemName = entry.CatalogItem?.Name ?? "",
                Price = entry.CatalogItem?.Price ?? 0
            });
        }
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedPatient == null)
        {
            MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
            return;
        }

        SelectedEntryIds = _rows
            .Where(x => x.IsSelected)
            .Select(x => x.Id)
            .ToList();

        if (SelectedEntryIds.Count == 0)
        {
            MessageBox.Show("Bitte mindestens eine Leistung auswählen.");
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public class BillableMedicalRecordEntryRow
{
    public bool IsSelected { get; set; }

    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public MedicalRecordEntryType EntryType { get; set; }

    public string Title { get; set; } = string.Empty;

    public string CatalogItemName { get; set; } = string.Empty;

    public decimal Price { get; set; }
}