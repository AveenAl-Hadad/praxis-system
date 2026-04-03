using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Praxis.Domain.Entities;

namespace Praxis.Client.Views.Pages.Patienten
{
    public partial class PatientEditPage : UserControl
    {
        private List<Patient> _allPatients = new();
        private Patient? _currentPatient;

        public PatientEditPage()
        {
            InitializeComponent();
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
                if (Application.Current.MainWindow is not MainWindow mainWindow)
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
                MessageBox.Show(
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
        }

        private void SetComboBoxByContent(ComboBox comboBox, string value)
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
                    MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
                    return;
                }

                if (Application.Current.MainWindow is not MainWindow mainWindow)
                    return;

                var vorname = VornameTextBox.Text?.Trim();
                var nachname = NachnameTextBox.Text?.Trim();
                var geburtsdatum = GeburtsdatumPicker.SelectedDate;

                if (string.IsNullOrWhiteSpace(vorname))
                {
                    MessageBox.Show("Bitte Vorname eingeben.");
                    VornameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(nachname))
                {
                    MessageBox.Show("Bitte Nachname eingeben.");
                    NachnameTextBox.Focus();
                    return;
                }

                if (geburtsdatum == null)
                {
                    MessageBox.Show("Bitte Geburtsdatum auswählen.");
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

                MessageBox.Show("Patient wurde erfolgreich aktualisiert.",
                    "Erfolg",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,
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
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                await mainWindow.OpenPatientSearchPageAsync();
            }
        }
    }
}