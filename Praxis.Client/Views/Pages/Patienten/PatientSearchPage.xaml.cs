using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Praxis.Domain.Entities;

namespace Praxis.Client.Views.Pages.Patienten
{
    public partial class PatientSearchPage : UserControl
    {
        private List<Patient> _allPatients = new();

        public PatientSearchPage()
        {
            InitializeComponent();
            Loaded += PatientSearchPage_Loaded;
        }

        private async void PatientSearchPage_Loaded(object sender, RoutedEventArgs e)
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
                _allPatients = patients.ToList();

                PatientsGrid.ItemsSource = _allPatients;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Laden der Patientendaten:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadPatientsAsync();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var search = SearchTextBox.Text?.Trim().ToLower() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(search))
            {
                PatientsGrid.ItemsSource = _allPatients;
                return;
            }

            var filtered = _allPatients
                .Where(p =>
                    (p.Vorname?.ToLower().Contains(search) ?? false) ||
                    (p.Nachname?.ToLower().Contains(search) ?? false) ||
                    (p.Email?.ToLower().Contains(search) ?? false) ||
                    (p.Telefonnummer?.ToLower().Contains(search) ?? false) ||
                    p.Id.ToString().Contains(search) ||
                    p.Geburtsdatum.ToString("dd.MM.yyyy").Contains(search))
                .ToList();

            PatientsGrid.ItemsSource = filtered;
        }

        public Patient? GetSelectedPatient()
        {
            return PatientsGrid.SelectedItem as Patient;
        }

        private async void PatientsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (PatientsGrid.SelectedItem is not Patient selectedPatient)
                    return;

                var result = MessageBox.Show(
                    $"Was möchtest du für '{selectedPatient.FullName}' öffnen?\n\nJa = Dokumente\nNein = Termine\nAbbrechen = Nichts",
                    "Patientenaktion",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (Application.Current.MainWindow is not MainWindow mainWindow)
                    return;

                if (result == MessageBoxResult.Yes)
                {
                    await mainWindow.OpenPatientDocumentsPageAsync(selectedPatient);
                }
                else if (result == MessageBoxResult.No)
                {
                    await mainWindow.OpenPatientAppointmentsPageAsync(selectedPatient);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Öffnen der Patientenaktion:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void PatientsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatientsGrid.SelectedItem is Patient selectedPatient &&
                Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.SetSelectedPatient(selectedPatient);
            }
        }
    }
}