using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using Microsoft.Win32;
using System.Text;
using System.IO;
using System.Linq;

namespace Praxis.Client;

public partial class MainWindow : Window
{
    private readonly PatientService _patientService;

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
            List<Patient> patients = await _patientService.GetAllPatientsAsync();
            PatientsGrid.ItemsSource = patients;
            StatusText.Text = $"Anzahl Patienten: {patients.Count}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Fehler beim Laden.";
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadPatientsAsync();
    }
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
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Fehler beim Speichern ❌";
        }
    }


    private async void DeletePatient_Click(object sender, RoutedEventArgs e)
    {
    try
    {
        if (PatientsGrid.SelectedItem is Patient selected)
        {
            var result = MessageBox.Show(
                $"Patient {selected.Nachname} wirklich löschen?",
                "Bestätigung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _patientService.DeletePatientAsync(selected.Id);
                await LoadPatientsAsync();
            }
        }
        else
        {
            MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
        }
    }

    catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Fehler beim Speichern ❌";
        }

    }
private async void EditPatient_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (PatientsGrid.SelectedItem is Patient selected)
            {
                var dlg = new AddPatientWindow(selected)
                {
                    Owner = this
                };

                var ok = dlg.ShowDialog();
                if (ok == true && dlg.CreatedPatient != null)
                {
                    await _patientService.UpdatePatientAsync(dlg.CreatedPatient);
                    await LoadPatientsAsync();
                }
            }
            else
            {
                MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Fehler beim Speichern ❌";
        }
    }

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var term = SearchBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(term))
        {
            await LoadPatientsAsync();
        }
        else
        {
            var results = await _patientService.SearchPatientsAsync(term);
            PatientsGrid.ItemsSource = results;
            StatusText.Text = $"Gefundene Patienten: {results.Count}";
        }
    }
    private async void ToggleActive_Click(object sender, RoutedEventArgs e)
    {
        if (PatientsGrid.SelectedItem is Patient selected)
        {
            await _patientService.ToggleActiveAsync(selected.Id);
            await LoadPatientsAsync();
            StatusText.Text = "Status geändert ✅";
        }
        else
        {
            MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
        }
    }
    private void PatientsGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (PatientsGrid.SelectedItem is Patient selected)
        {
            var detail = new PatientDetailWindow(selected)
            {
                Owner = this
            };

            detail.ShowDialog();
        }
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 1) Aktuell angezeigte Liste holen (inkl. Suche/Filter)
            var current = PatientsGrid.ItemsSource as System.Collections.IEnumerable;

            if (current == null)
            {
                MessageBox.Show("Keine Daten zum Exportieren.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var patients = current.Cast<Patient>().ToList();
            if (patients.Count == 0)
            {
                MessageBox.Show("Keine Daten zum Exportieren.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 2) Dateidialog
            var dlg = new SaveFileDialog
            {
                Title = "Patientenliste exportieren",
                Filter = "CSV Datei (*.csv)|*.csv",
                FileName = $"patienten_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (dlg.ShowDialog() != true)
                return;

            // 3) CSV bauen
            var sb = new StringBuilder();
            sb.AppendLine("Id;Nachname;Vorname;Geburtsdatum;Alter;Email;Telefon;Status");

            foreach (var p in patients)
            {
                var status = p.IsActive ? "Aktiv" : "Inaktiv";

                // CSV-safe: Semikolon und Zeilenumbrüche entfernen/ersetzen
                string Clean(string? s) =>
                    (s ?? "").Replace(";", ",").Replace("\r", " ").Replace("\n", " ").Trim();

                sb.AppendLine(
                    $"{p.Id};{Clean(p.Nachname)};{Clean(p.Vorname)};{p.Geburtsdatum:yyyy-MM-dd};{p.Alter};{Clean(p.Email)};{Clean(p.Telefonnummer)};{status}"
                );
            }

            // 4) Datei schreiben (UTF-8)
            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);

            StatusText.Text = $"CSV exportiert: {dlg.FileName}";
            MessageBox.Show("Export erfolgreich ✅", "CSV Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export fehlgeschlagen:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

}