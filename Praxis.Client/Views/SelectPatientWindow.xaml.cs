using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Praxis.Domain.Entities;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views;

public partial class SelectPatientWindow : Window
{
    private readonly List<Patient> _allPatients;
    private readonly ObservableCollection<Patient> _filteredPatients = new();

    public Patient? SelectedPatient { get; private set; }

    public SelectPatientWindow(IEnumerable<Patient> patients)
    {
        InitializeComponent();

        _allPatients = patients
            .Where(x => x.IsActive)
            .OrderBy(x => x.Nachname)
            .ThenBy(x => x.Vorname)
            .ToList();

        PatientsGrid.ItemsSource = _filteredPatients;

        ApplyFilter();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var search = SearchTextBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;

        _filteredPatients.Clear();

        var result = _allPatients.Where(p =>
            string.IsNullOrWhiteSpace(search)
            || (p.Nachname ?? string.Empty).ToLowerInvariant().Contains(search)
            || (p.Vorname ?? string.Empty).ToLowerInvariant().Contains(search)
            || p.Geburtsdatum.ToString("dd.MM.yyyy").Contains(search)
            || (p.Telefonnummer ?? string.Empty).ToLowerInvariant().Contains(search)
            || (p.Email ?? string.Empty).ToLowerInvariant().Contains(search));

        foreach (var patient in result)
            _filteredPatients.Add(patient);
    }

    private void SelectCurrentPatient()
    {
        if (PatientsGrid.SelectedItem is not Patient patient)
        {
            MessageBox.Show("Bitte einen Patienten auswählen.");
            return;
        }

        SelectedPatient = patient;
        DialogResult = true;
        Close();
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        SelectCurrentPatient();
    }

    private void PatientsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        SelectCurrentPatient();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}