using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Praxis.Domain.Entities;

namespace Praxis.Client.Views.Pages.Patienten
{
    public partial class PatientDeletePage : System.Windows.Controls.UserControl
    {
        private List<Patient> _allPatients = new();
        private Patient? _currentPatient;

        public PatientDeletePage()
        {
            InitializeComponent();
            Loaded += PatientDeletePage_Loaded;
        }

        private async void PatientDeletePage_Loaded(object sender, RoutedEventArgs e)
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
                _allPatients = patients
                    .OrderBy(p => p.Nachname)
                    .ThenBy(p => p.Vorname)
                    .ToList();

                PatientComboBox.ItemsSource = _allPatients;

                if (_allPatients.Count > 0)
                    PatientComboBox.SelectedIndex = 0;
                else
                    ClearForm();
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
            OrtTextBox.Text = $"{patient.PLZ} {patient.Ort}".Trim();
        }

        private void ClearForm()
        {
            _currentPatient = null;
            VornameTextBox.Clear();
            NachnameTextBox.Clear();
            GeburtsdatumPicker.SelectedDate = null;
            TelefonTextBox.Clear();
            EmailTextBox.Clear();
            AdresseTextBox.Clear();
            OrtTextBox.Clear();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
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

                var result = System.Windows.MessageBox.Show(
                    $"Patient '{_currentPatient.FullName}' wirklich löschen?\n\nDiese Aktion kann nicht rückgängig gemacht werden.",
                    "Sicherheitsabfrage",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                await mainWindow.DeletePatientByIdAsync(_currentPatient.Id);
                await mainWindow.OpenPatientSearchPageAsync();

                System.Windows.MessageBox.Show("Patient wurde gelöscht.",
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

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                await mainWindow.OpenPatientSearchPageAsync();
            }
        }
    }
}