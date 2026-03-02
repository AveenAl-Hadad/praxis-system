// MainWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
// optional, falls du UserFriendlyException nutzt:
// using Praxis.Infrastructure.Exceptions;

namespace Praxis.Client;

public partial class MainWindow : Window
{
    private readonly PatientService _patientService;

    private List<Patient> _allPatients = new();

    public MainWindow(PatientService patientService)
    {
        InitializeComponent();
        _patientService = patientService;

        Loaded += async (_, __) => await LoadPatientsAsync();
    }

    private async Task LoadPatientsAsync()
    {
        try
        {
            StatusText.Text = "Lade Patienten...";
            _allPatients = (await _patientService.GetAllPatientsAsync()).ToList();
            ApplyFilters();
            StatusText.Text = $"Geladen: {_allPatients.Count} Patienten";
        }
        // catch (UserFriendlyException ex)
        // {
        //     MessageBox.Show(ex.Message, "Hinweis", MessageBoxButton.OK, MessageBoxImage.Warning);
        // }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Fehler beim Laden ❌";
        }
    }

    private void ApplyFilters()
    {
        if (PatientsGrid == null)
        {
            MessageBox.Show("PatientsGrid ist NULL -> XAML wurde nicht richtig geladen/kompiliert");
            return;
        }
        var term = (SearchBox.Text ?? "").Trim().ToLower();
        var onlyActive = OnlyActiveCheck.IsChecked == true;

        IEnumerable<Patient> query = _allPatients;

        if (onlyActive)
            query = query.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(p =>
                (p.Nachname ?? "").ToLower().Contains(term) ||
                (p.Vorname ?? "").ToLower().Contains(term) ||
                (p.Email ?? "").ToLower().Contains(term) ||
                (p.Telefonnummer ?? "").ToLower().Contains(term));
        }

        var list = query.ToList();

        // Wichtig: nur ItemsSource nutzen, niemals PatientsGrid.Items.Add(...)
        PatientsGrid.ItemsSource = null;
        PatientsGrid.ItemsSource = list;

        StatusText.Text = $"Anzeige: {list.Count} / Gesamt: {_allPatients.Count}";
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => ApplyFilters();

    private void OnlyActiveCheck_Changed(object sender, RoutedEventArgs e)
        => ApplyFilters();

    private async void Refresh_Click(object sender, RoutedEventArgs e)
        => await LoadPatientsAsync();

    private async void AddPatient_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new AddPatientWindow { Owner = this };
            if (dlg.ShowDialog() == true && dlg.CreatedPatient != null)
            {
                await _patientService.AddPatientAsync(dlg.CreatedPatient);
                await LoadPatientsAsync();
                StatusText.Text = "Patient gespeichert ✅";
            }
        }
        // catch (UserFriendlyException ex)
        // {
        //     MessageBox.Show(ex.Message, "Hinweis", MessageBoxButton.OK, MessageBoxImage.Warning);
        // }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Fehler beim Speichern ❌";
        }
    }

    private async void EditPatient_Click(object sender, RoutedEventArgs e)
    {
        if (PatientsGrid.SelectedItem is not Patient selected)
        {
            MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
            return;
        }

        try
        {
            // Wir erstellen eine Kopie fürs Bearbeiten, damit EF/WPF nicht mit Tracking kollidiert
            var copy = new Patient
            {
                Id = selected.Id,
                Vorname = selected.Vorname,
                Nachname = selected.Nachname,
                Geburtsdatum = selected.Geburtsdatum,
                Email = selected.Email,
                Telefonnummer = selected.Telefonnummer,
                IsActive = selected.IsActive
            };

            var dlg = new AddPatientWindow(copy) { Owner = this };
            if (dlg.ShowDialog() == true && dlg.CreatedPatient != null)
            {
                await _patientService.UpdatePatientAsync(dlg.CreatedPatient);
                await LoadPatientsAsync();
                StatusText.Text = "Patient aktualisiert ✅";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Fehler beim Bearbeiten ❌";
        }
    }

    private async void DeletePatient_Click(object sender, RoutedEventArgs e)
    {
        if (PatientsGrid.SelectedItem is not Patient selected)
        {
            MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
            return;
        }

        var result = MessageBox.Show(
            $"Patient {selected.Nachname}, {selected.Vorname} wirklich löschen?",
            "Bestätigung",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _patientService.DeletePatientAsync(selected.Id);
            await LoadPatientsAsync();
            StatusText.Text = "Patient gelöscht ✅";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Fehler beim Löschen ❌";
        }
    }

    private async void ToggleActive_Click(object sender, RoutedEventArgs e)
    {
        if (PatientsGrid.SelectedItem is not Patient selected)
        {
            MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
            return;
        }

        try
        {
            await _patientService.ToggleActiveAsync(selected.Id);
            await LoadPatientsAsync();
            StatusText.Text = "Status geändert ✅";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Fehler beim Statuswechsel ❌";
        }
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (PatientsGrid.ItemsSource is not IEnumerable<Patient> patientsEnum)
            {
                MessageBox.Show("Keine Daten zum Exportieren.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var patients = patientsEnum.ToList();
            if (patients.Count == 0)
            {
                MessageBox.Show("Keine Daten zum Exportieren.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title = "Patientenliste exportieren",
                Filter = "CSV Datei (*.csv)|*.csv",
                FileName = $"patienten_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (dlg.ShowDialog() != true) return;

            string Clean(string? s) =>
                (s ?? "").Replace(";", ",").Replace("\r", " ").Replace("\n", " ").Trim();

            var sb = new StringBuilder();
            sb.AppendLine("Id;Nachname;Vorname;Geburtsdatum;Alter;Email;Telefon;Status");

            foreach (var p in patients)
            {
                var status = p.IsActive ? "Aktiv" : "Inaktiv";
                sb.AppendLine($"{p.Id};{Clean(p.Nachname)};{Clean(p.Vorname)};{p.Geburtsdatum:yyyy-MM-dd};{p.Alter};{Clean(p.Email)};{Clean(p.Telefonnummer)};{status}");
            }

            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);

            StatusText.Text = $"CSV exportiert: {dlg.FileName}";
            MessageBox.Show("Export erfolgreich ✅", "CSV Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export fehlgeschlagen:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PatientsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (PatientsGrid.SelectedItem is not Patient selected) return;

        // Detail-Fenster muss existieren:
        // PatientDetailWindow(Patient patient)
        var wnd = new PatientDetailWindow(selected) { Owner = this };
        wnd.ShowDialog();
    }

    // Shortcuts
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
        {
            AddPatient_Click(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
        {
            ExportCsv_Click(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F5)
        {
            Refresh_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void PatientsGrid_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            DeletePatient_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        // Strg+F fokusiert schon über Window_KeyDown, aber wir lassen es hier auch zu
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
    }
}