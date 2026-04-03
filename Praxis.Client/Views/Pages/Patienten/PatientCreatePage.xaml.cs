using System;
using System.Windows;
using System.Windows.Controls;
using Praxis.Domain.Entities;

namespace Praxis.Client.Views.Pages.Patienten
{
    public partial class PatientCreatePage : UserControl
    {
        public PatientCreatePage()
        {
            InitializeComponent();
            Loaded += PatientCreatePage_Loaded;
        }

        private void PatientCreatePage_Loaded(object sender, RoutedEventArgs e)
        {
            if (GeburtsdatumPicker.SelectedDate == null)
                GeburtsdatumPicker.SelectedDate = DateTime.Today;

            if (VersicherungComboBox.SelectedIndex < 0)
                VersicherungComboBox.SelectedIndex = 0;

            if (GeschlechtComboBox.SelectedIndex < 0)
                GeschlechtComboBox.SelectedIndex = 0;

            if (IsActiveCheckBox.IsChecked == null)
                IsActiveCheckBox.IsChecked = true;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Application.Current.MainWindow is not MainWindow mainWindow)
                    return;

                var vorname = VornameTextBox.Text?.Trim();
                var nachname = NachnameTextBox.Text?.Trim();
                var geburtsdatum = GeburtsdatumPicker.SelectedDate;
                var telefon = TelefonTextBox.Text?.Trim();
                var email = EmailTextBox.Text?.Trim();

                var adresse = AdresseTextBox.Text?.Trim();
                var plz = PLZTextBox.Text?.Trim();
                var ort = OrtTextBox.Text?.Trim();
                var versichertennummer = VersichertennummerTextBox.Text?.Trim();

                var versicherung = (VersicherungComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
                var geschlecht = (GeschlechtComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

                var isActive = IsActiveCheckBox.IsChecked == true;

                if (string.IsNullOrWhiteSpace(vorname))
                {
                    MessageBox.Show("Bitte Vorname eingeben.",
                        "Validierung",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    VornameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(nachname))
                {
                    MessageBox.Show("Bitte Nachname eingeben.",
                        "Validierung",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    NachnameTextBox.Focus();
                    return;
                }

                if (geburtsdatum == null)
                {
                    MessageBox.Show("Bitte Geburtsdatum auswählen.",
                        "Validierung",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(email) && !email.Contains("@"))
                {
                    MessageBox.Show("Bitte eine gültige E-Mail-Adresse eingeben.",
                        "Validierung",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    EmailTextBox.Focus();
                    return;
                }

                var patient = new Patient
                {
                    Vorname = vorname,
                    Nachname = nachname,
                    Geburtsdatum = geburtsdatum.Value,
                    Telefonnummer = telefon ?? string.Empty,
                    Email = email ?? string.Empty,
                    IsActive = isActive,

                    Adresse = adresse ?? string.Empty,
                    PLZ = plz ?? string.Empty,
                    Ort = ort ?? string.Empty,
                    Versicherung = versicherung,
                    Geschlecht = geschlecht,
                    Versichertennummer = versichertennummer ?? string.Empty
                };

                await mainWindow.CreatePatientAsync(patient);
                await mainWindow.OpenPatientSearchPageAsync();

                MessageBox.Show("Patient wurde erfolgreich angelegt.",
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
            VornameTextBox.Clear();
            NachnameTextBox.Clear();
            TelefonTextBox.Clear();
            EmailTextBox.Clear();

            AdresseTextBox.Clear();
            PLZTextBox.Clear();
            OrtTextBox.Clear();
            VersichertennummerTextBox.Clear();

            GeburtsdatumPicker.SelectedDate = DateTime.Today;

            VersicherungComboBox.SelectedIndex = 0;
            GeschlechtComboBox.SelectedIndex = 0;

            IsActiveCheckBox.IsChecked = true;
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                await mainWindow.OpenPatientSearchPageAsync();
            }
        }
    }
}