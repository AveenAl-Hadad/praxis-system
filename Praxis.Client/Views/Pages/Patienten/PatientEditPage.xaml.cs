using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Praxis.Domain.Entities;
using Praxis.Application.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views.Pages.Patienten
{
    public partial class PatientEditPage : System.Windows.Controls.UserControl
    {
        private List<Patient> _allPatients = new();
        private Patient? _currentPatient;
        private readonly IPatientDiagnosisService _patientDiagnosisService;
        private readonly ObservableCollection<PatientDiagnosis> _diagnoses = new();
        private CatalogItem? _selectedDiagnosisCatalogItem;

        public PatientEditPage(IPatientDiagnosisService patientDiagnosisService)
        {
            InitializeComponent();

            _patientDiagnosisService = patientDiagnosisService;
            DiagnosesGrid.ItemsSource = _diagnoses;

            Loaded += PatientEditPage_Loaded;
        }

        private async void PatientEditPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadPatientsAsync();
        }

        public async Task RefreshAsync()
        {
            await LoadPatientsAsync();
        }

        private async Task LoadPatientsAsync()
        {
            try
            {
                if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                    return;

                var patients = await mainWindow.GetPatientsAsync();
                _allPatients = patients.OrderBy(p => p.Nachname).ThenBy(p => p.Vorname).ToList();

                PatientComboBox.ItemsSource = _allPatients;

                if (_allPatients.Count > 0 && PatientComboBox.SelectedItem == null)
                {
                    PatientComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Fehler beim Laden der Patienten:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        public async Task LoadPatientAsync(Patient patient)
        {
            await LoadPatientsAsync();

            var match = _allPatients.FirstOrDefault(p => p.Id == patient.Id);
            if (match != null)
            {
                PatientComboBox.SelectedItem = match;
                _currentPatient = match;
                FillForm(match);
            }
        }

        private void PatientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatientComboBox.SelectedItem is Patient patient)
            {
                _currentPatient = patient;
                FillForm(patient);
            }
        }

        private void FillForm(Patient patient)
        {
            VornameTextBox.Text = patient.Vorname;
            NachnameTextBox.Text = patient.Nachname;
            GeburtsdatumPicker.SelectedDate = patient.Geburtsdatum;
            TelefonTextBox.Text = patient.Telefonnummer;
            EmailTextBox.Text = patient.Email;

            AdresseTextBox.Text = patient.Adresse;
            PLZTextBox.Text = patient.PLZ;
            OrtTextBox.Text = patient.Ort;
            VersichertennummerTextBox.Text = patient.Versichertennummer;
            IsActiveCheckBox.IsChecked = patient.IsActive;

            SetComboBoxByContent(VersicherungComboBox, patient.Versicherung);
            SetComboBoxByContent(GeschlechtComboBox, patient.Geschlecht);
            _ = LoadDiagnosesAsync(patient.Id);
        }

        //Diagnosen laden
        private async Task LoadDiagnosesAsync(int patientId)
        {
            var diagnoses = await _patientDiagnosisService.GetByPatientAsync(patientId);

            _diagnoses.Clear();

            foreach (var diagnosis in diagnoses)
                _diagnoses.Add(diagnosis);
        }
        private void SetComboBoxByContent(System.Windows.Controls.ComboBox comboBox, string value)
        {
            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem cbItem &&
                    string.Equals(cbItem.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = cbItem;
                    return;
                }
            }

            comboBox.SelectedIndex = -1;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentPatient == null)
                {
                    System.Windows.MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
                    return;
                }

                if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                    return;

                var vorname = VornameTextBox.Text?.Trim();
                var nachname = NachnameTextBox.Text?.Trim();
                var geburtsdatum = GeburtsdatumPicker.SelectedDate;

                if (string.IsNullOrWhiteSpace(vorname))
                {
                    System.Windows.MessageBox.Show("Bitte Vorname eingeben.");
                    VornameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(nachname))
                {
                    System.Windows.MessageBox.Show("Bitte Nachname eingeben.");
                    NachnameTextBox.Focus();
                    return;
                }

                if (geburtsdatum == null)
                {
                    System.Windows.MessageBox.Show("Bitte Geburtsdatum auswählen.");
                    return;
                }

                var updatedPatient = new Patient
                {
                    Id = _currentPatient.Id,
                    Vorname = vorname,
                    Nachname = nachname,
                    Geburtsdatum = geburtsdatum.Value,
                    Telefonnummer = TelefonTextBox.Text?.Trim() ?? string.Empty,
                    Email = EmailTextBox.Text?.Trim() ?? string.Empty,
                    IsActive = IsActiveCheckBox.IsChecked == true,

                    Adresse = AdresseTextBox.Text?.Trim() ?? string.Empty,
                    PLZ = PLZTextBox.Text?.Trim() ?? string.Empty,
                    Ort = OrtTextBox.Text?.Trim() ?? string.Empty,
                    Versicherung = (VersicherungComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                    Geschlecht = (GeschlechtComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                    Versichertennummer = VersichertennummerTextBox.Text?.Trim() ?? string.Empty
                };

                await mainWindow.UpdatePatientAysnc(updatedPatient);
                await mainWindow.OpenPatientSearchPageAsync();

                System.Windows.MessageBox.Show("Patient wurde erfolgreich aktualisiert.",
                    "Erfolg",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message,
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPatient != null)
            {
                FillForm(_currentPatient);
            }
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                await mainWindow.OpenPatientSearchPageAsync();
            }
        }

        //Autocomplete-Code hinzufügen
        private async void DiagnosisSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var search = DiagnosisSearchBox.Text?.Trim() ?? "";

            if (search.Length < 2)
            {
                DiagnosisSuggestionList.Visibility = Visibility.Collapsed;
                DiagnosisSuggestionList.ItemsSource = null;
                return;
            }

            var result = await _patientDiagnosisService.SearchIcdAsync(search);

            var suggestions = result
                .Select(x => new DiagnosisSuggestion { Item = x })
                .ToList();

            DiagnosisSuggestionList.ItemsSource = suggestions;
            DiagnosisSuggestionList.Visibility = suggestions.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void DiagnosisSuggestionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectDiagnosisSuggestion();
        }

        private void DiagnosisSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SelectDiagnosisSuggestion();
                e.Handled = true;
            }
        }

        private void SelectDiagnosisSuggestion()
        {
            if (DiagnosisSuggestionList.SelectedItem is not DiagnosisSuggestion suggestion)
                return;

            _selectedDiagnosisCatalogItem = suggestion.Item;
            DiagnosisSearchBox.Text = $"{suggestion.Item.Code} - {suggestion.Item.Name}";
            DiagnosisSuggestionList.Visibility = Visibility.Collapsed;
        }

        //Hinzufügen/Löschen
        private async void AddDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentPatient == null)
                {
                    MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
                    return;
                }

                if (_selectedDiagnosisCatalogItem == null)
                {
                    MessageBox.Show("Bitte zuerst eine ICD-Diagnose auswählen.");
                    return;
                }

                await _patientDiagnosisService.AddAsync(
                    _currentPatient.Id,
                    _selectedDiagnosisCatalogItem.Id,
                    DiagnosisNotesBox.Text);

                DiagnosisSearchBox.Clear();
                DiagnosisNotesBox.Clear();
                _selectedDiagnosisCatalogItem = null;

                await LoadDiagnosesAsync(_currentPatient.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Diagnose Fehler");
            }
        }

        private async void DeleteDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            if (DiagnosesGrid.SelectedItem is not PatientDiagnosis diagnosis)
            {
                MessageBox.Show("Bitte zuerst eine Diagnose auswählen.");
                return;
            }

            await _patientDiagnosisService.DeleteAsync(diagnosis.Id);

            if (_currentPatient != null)
                await LoadDiagnosesAsync(_currentPatient.Id);
        }
    }
    public class DiagnosisSuggestion
    {
        public CatalogItem Item { get; set; } = null!;
        public string DisplayText => $"{Item.Code} - {Item.Name}";
    }
}