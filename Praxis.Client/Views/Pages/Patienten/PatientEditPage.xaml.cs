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
using Praxis.Client.Security;

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

        private readonly IPatientMedicationService _patientMedicationService;
        private readonly ObservableCollection<PatientMedication> _medications = new();
        private CatalogItem? _selectedMedicationCatalogItem;

        public PatientEditPage(
                                 IPatientDiagnosisService patientDiagnosisService,
                                 IPatientMedicationService patientMedicationService)
        {
            InitializeComponent();

            _patientDiagnosisService = patientDiagnosisService;
            _patientMedicationService = patientMedicationService;

            DiagnosesGrid.ItemsSource = _diagnoses;
            MedicationsGrid.ItemsSource = _medications;

            DeleteDiagnosisButton.Visibility = PermissionHelper.CanDeletePatients
                ? Visibility.Visible
                : Visibility.Collapsed;

            DeleteMedicationButton.Visibility = PermissionHelper.CanDeletePatients
                ? Visibility.Visible
                : Visibility.Collapsed;

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
            _ = LoadMedicationsAsync(patient.Id);
        }
        // Medikament laden
        private async Task LoadMedicationsAsync(int patientId)
        {
            var medications = await _patientMedicationService.GetByPatientAsync(patientId);

            _medications.Clear();

            foreach (var medication in medications)
                _medications.Add(medication);
        }
        //Autocomplete für Medikamente
        private async void MedicationSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var search = MedicationSearchBox.Text?.Trim() ?? "";

            if (search.Length < 2)
            {
                MedicationSuggestionList.Visibility = Visibility.Collapsed;
                MedicationSuggestionList.ItemsSource = null;
                return;
            }

            var result = await _patientMedicationService.SearchMedicationAsync(search);

            var suggestions = result
                .Select(x => new MedicationSuggestion { Item = x })
                .ToList();

            MedicationSuggestionList.ItemsSource = suggestions;
            MedicationSuggestionList.Visibility = suggestions.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void MedicationSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (MedicationSuggestionList.Visibility != Visibility.Visible)
                return;

            if (e.Key == Key.Down)
            {
                MedicationSuggestionList.Focus();

                if (MedicationSuggestionList.Items.Count > 0)
                    MedicationSuggestionList.SelectedIndex = 0;

                e.Handled = true;
            }

            if (e.Key == Key.Enter)
            {
                if (MedicationSuggestionList.Items.Count > 0 &&
                    MedicationSuggestionList.SelectedIndex < 0)
                {
                    MedicationSuggestionList.SelectedIndex = 0;
                }

                SelectMedicationSuggestion();
                e.Handled = true;
            }
        }

        private void MedicationSuggestionList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SelectMedicationSuggestion();
                e.Handled = true;
            }
        }

        private void MedicationSuggestionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectMedicationSuggestion();
        }

        private void SelectMedicationSuggestion()
        {
            if (MedicationSuggestionList.SelectedItem is not MedicationSuggestion suggestion)
                return;

            _selectedMedicationCatalogItem = suggestion.Item;

            MedicationSearchBox.Text = $"{suggestion.Item.Code} - {suggestion.Item.Name}";
            MedicationSuggestionList.Visibility = Visibility.Collapsed;
            MedicationDosageBox.Focus();
        }

        // Speichern und Löschen
        private async void AddMedication_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentPatient == null)
                {
                    MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
                    return;
                }

                if (_selectedMedicationCatalogItem == null)
                {
                    MessageBox.Show("Bitte zuerst ein Medikament auswählen.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(MedicationDosageBox.Text))
                {
                    MessageBox.Show("Bitte Dosierung eingeben, z.B. 1-0-1.");
                    return;
                }

                await _patientMedicationService.AddAsync(
                    _currentPatient.Id,
                    _selectedMedicationCatalogItem.Id,
                    MedicationDosageBox.Text,
                    MedicationNotesBox.Text);

                MedicationSearchBox.Clear();
                MedicationDosageBox.Clear();
                MedicationNotesBox.Clear();
                _selectedMedicationCatalogItem = null;

                await LoadMedicationsAsync(_currentPatient.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Medikament Fehler");
            }
        }

        private async void DeleteMedication_Click(object sender, RoutedEventArgs e)
        {
            if (MedicationsGrid.SelectedItem is not PatientMedication medication)
            {
                MessageBox.Show("Bitte zuerst eine Verordnung auswählen.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Verordnung {medication.CatalogItem.Name} wirklich löschen?",
                "Verordnung löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            await _patientMedicationService.DeleteAsync(medication.Id);

            if (_currentPatient != null)
                await LoadMedicationsAsync(_currentPatient.Id);
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
            if (DiagnosisSuggestionList.Visibility != Visibility.Visible)
                return;

            if (e.Key == Key.Down)
            {
                DiagnosisSuggestionList.Focus();

                if (DiagnosisSuggestionList.Items.Count > 0)
                    DiagnosisSuggestionList.SelectedIndex = 0;

                e.Handled = true;
            }

            if (e.Key == Key.Enter)
            {
                if (DiagnosisSuggestionList.Items.Count > 0 &&
                    DiagnosisSuggestionList.SelectedIndex < 0)
                {
                    DiagnosisSuggestionList.SelectedIndex = 0;
                }

                SelectDiagnosisSuggestion();
                e.Handled = true;
            }
        }
        private void DiagnosisSuggestionList_KeyDown(object sender, KeyEventArgs e)
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

            var confirm = MessageBox.Show(
                $"Diagnose {diagnosis.CatalogItem.Code} wirklich löschen?",
                "Diagnose löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            await _patientDiagnosisService.DeleteAsync(diagnosis.Id);

            if (_currentPatient != null)
                await LoadDiagnosesAsync(_currentPatient.Id);
        }

        private void PrintPrescription_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPatient == null)
            {
                MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
                return;
            }

            if (_medications.Count == 0)
            {
                MessageBox.Show("Keine Medikamente für diesen Patienten vorhanden.");
                return;
            }

            var window = new PrescriptionPreviewWindow(_currentPatient, _medications)
            {
                Owner = Window.GetWindow(this)
            };

            window.ShowDialog();
        }

    }
    public class DiagnosisSuggestion
    {
        public CatalogItem Item { get; set; } = null!;
        public string DisplayText => $"{Item.Code} - {Item.Name}";
    }
    public class MedicationSuggestion
    {
        public CatalogItem Item { get; set; } = null!;
        public string DisplayText => $"{Item.Code} - {Item.Name}";
    }
}