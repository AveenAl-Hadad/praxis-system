using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using System.ComponentModel;

namespace Praxis.Client;

public partial class MainWindow : Window
{
    private readonly PatientService _patientService;

    private List<Patient> _allPatients = new();
    private List<Patient> _filteredPatients = new();

    // Sortierung (per Header)
    private string _sortBy = nameof(Patient.Nachname);
    private ListSortDirection _sortDir = ListSortDirection.Ascending;

    // Pagination
    private const int PageSize = 50;
    private int _currentPage = 1;
    private int _totalPages = 1;

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

            _currentPage = 1;
            ApplyFilterSortAndPagination();

            StatusText.Text = $"Geladen: {_allPatients.Count} Patienten";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Fehler beim Laden ❌";
        }
    }

    private void ApplyFilterSortAndPagination()
    {
        if (PatientsGrid == null) return;
        // 1) Filter
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

        _filteredPatients = query.ToList();

        // 2) Sort
        _filteredPatients = SortPatients(_filteredPatients, _sortBy, _sortDir);

        // 3) Pagination berechnen
        _totalPages = Math.Max(1, (int)Math.Ceiling(_filteredPatients.Count / (double)PageSize));

        //wenn Page außerhalb liegt -> korrigieren
        if (_currentPage < 1) _currentPage = 1;
        if (_currentPage > _totalPages) _currentPage = _totalPages;

        var pageItems = _filteredPatients
            .Skip((_currentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        PatientsGrid.ItemsSource = null;
        PatientsGrid.ItemsSource = pageItems;

        PageInfoText.Text = $"Seite {_currentPage} / {_totalPages} (je {PageSize})";
        StatusText.Text = $"Anzeige: {pageItems.Count} | Gefiltert: {_filteredPatients.Count} | Gesamt: {_allPatients.Count} | Sort: {_sortBy} ({(_sortDir == ListSortDirection.Ascending ? "A→Z" : "Z→A")})";
    }

    private static List<Patient> SortPatients(List<Patient> list, string sortBy, ListSortDirection dir)
    {
        bool asc = dir == ListSortDirection.Ascending;

        return sortBy switch
        {
            nameof(Patient.Id) => asc ? list.OrderBy(p => p.Id).ToList() : list.OrderByDescending(p => p.Id).ToList(),
            nameof(Patient.Nachname) => asc ? list.OrderBy(p => p.Nachname).ThenBy(p => p.Vorname).ToList()
                                           : list.OrderByDescending(p => p.Nachname).ThenBy(p => p.Vorname).ToList(),
            nameof(Patient.Vorname) => asc ? list.OrderBy(p => p.Vorname).ThenBy(p => p.Nachname).ToList()
                                          : list.OrderByDescending(p => p.Vorname).ThenBy(p => p.Nachname).ToList(),
            nameof(Patient.Geburtsdatum) => asc ? list.OrderBy(p => p.Geburtsdatum).ToList()
                                               : list.OrderByDescending(p => p.Geburtsdatum).ToList(),
            nameof(Patient.Alter) => asc ? list.OrderBy(p => p.Alter).ToList()
                                        : list.OrderByDescending(p => p.Alter).ToList(),
            nameof(Patient.Email) => asc ? list.OrderBy(p => p.Email).ToList()
                                        : list.OrderByDescending(p => p.Email).ToList(),
            nameof(Patient.Telefonnummer) => asc ? list.OrderBy(p => p.Telefonnummer).ToList()
                                                : list.OrderByDescending(p => p.Telefonnummer).ToList(),
            nameof(Patient.IsActive) => asc ? list.OrderBy(p => p.IsActive).ToList()
                                           : list.OrderByDescending(p => p.IsActive).ToList(),
            _ => list
        };
    }

    // Events: Filter
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _currentPage = 1;
        ApplyFilterSortAndPagination();
    }

    private void OnlyActiveCheck_Changed(object sender, RoutedEventArgs e)
    {
        _currentPage = 1;
        ApplyFilterSortAndPagination();
    }

    // Events: Pagination
    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage < _totalPages)
        {
            _currentPage++;
            ApplyFilterSortAndPagination();
        }
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            ApplyFilterSortAndPagination();
        }
    }

    // Events: Sortierung per Header
    private void PatientsGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;

        var sortBy = e.Column.SortMemberPath;
        if (string.IsNullOrWhiteSpace(sortBy)) return;

        // Toggle Richtung
        if (_sortBy == sortBy)
            _sortDir = _sortDir == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
        else
        {
            _sortBy = sortBy;
            _sortDir = ListSortDirection.Ascending;
        }

        // Pfeile korrekt setzen
        foreach (var col in PatientsGrid.Columns)
            col.SortDirection = null;

        e.Column.SortDirection = _sortDir;

        ApplyFilterSortAndPagination();
    }

    // Buttons
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadPatientsAsync();

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

    private async void EditPatient_Click(object sender, RoutedEventArgs e)
    {
        if (PatientsGrid.SelectedItem is not Patient selected)
        {
            MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
            return;
        }

        try
        {
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
            // Exportiert die aktuell gefilterte Liste (nicht nur Seite)
            if (_filteredPatients.Count == 0)
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

            foreach (var p in _filteredPatients)
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
}