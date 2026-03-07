using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Praxis.Client.Logic;          // <-- PatientListManager Namespace
using Praxis.Client.Logic.UI;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Client.Views;

namespace Praxis.Client;

public partial class MainWindow : Window
{
    private readonly IPatientService _patientService;


    private List<Patient> _allPatients = new();
    private PatientListManager? _listManager;
    private PatientCrudController _crud;

    // Filter-UI Zustand
    private string CurrentSearchTerm => (SearchBox.Text ?? "").Trim();
    private bool OnlyActive => OnlyActiveCheck.IsChecked == true;

    // Sort-Zustand (kommt von Header-Klick)
    private string _sortBy = nameof(Patient.Nachname);
    private ListSortDirection _sortDir = ListSortDirection.Ascending;

    public MainWindow(IPatientService patientService)
    {
        InitializeComponent();
        _patientService = patientService;
        var dialogService = new WpfDialogService();
        var messageService = new WpfMessageBoxService();
        _crud = new PatientCrudController(_patientService, dialogService, messageService);

        ContentRendered += async (_, __) => await LoadPatientsAsync();
    }

    /// <summary>
    /// Lädt alle Patienten aus der DB und initialisiert den ListManager.
    /// </summary>
    private async Task LoadPatientsAsync()
    {
        try
        {
            StatusText.Text = "Lade Patienten...";

            _allPatients = (await _patientService.GetAllPatientsAsync()).ToList();

            // ListManager initialisieren (Filter/Sort/Pagination)
            _listManager = new PatientListManager(_allPatients)
            {
                PageSize = 50
            };
            _listManager.SetSorting(_sortBy, _sortDir);

            RenderList(); // UI aktualisieren

            StatusText.Text = $"Geladen: {_allPatients.Count} Patienten";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Fehler beim Laden ❌";
        }
    }

    /// <summary>
    /// Rendert die aktuelle Seite in die DataGrid + aktualisiert Pagination-Info + Status.
    /// </summary>
    private void RenderList()
    {
        if (_listManager == null) return;

        var pageItems = _listManager.GetPage(CurrentSearchTerm, OnlyActive);

        PatientsGrid.ItemsSource = null;
        PatientsGrid.ItemsSource = pageItems;

        PageInfoText.Text = $"Seite {_listManager.CurrentPage} / {_listManager.TotalPages} (je {_listManager.PageSize})";

        // Hinweis: Gesamt = _allPatients.Count, Gefiltert können wir über Manager optional als Property bereitstellen.
        StatusText.Text = $"Anzeige: {pageItems.Count} | Gesamt: {_allPatients.Count} | Sort: {_sortBy} ({(_sortDir == ListSortDirection.Ascending ? "A→Z" : "Z→A")})";
    }

    // ------------------------
    // Filter Events
    // ------------------------

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_listManager == null) return;
        _listManager.GoToFirstPage();
        RenderList();
    }

    private void OnlyActiveCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_listManager == null) return;
        _listManager.GoToFirstPage();
        RenderList();
    }

    // ------------------------
    // Pagination Events
    // ------------------------

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (_listManager == null) return;
        _listManager.NextPage();
        RenderList();
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (_listManager == null) return;
        _listManager.PreviousPage();
        RenderList();
    }

    // ------------------------
    // Sortierung per Spaltenkopf
    // ------------------------

    private void PatientsGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (_listManager == null) return;

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

        // Pfeile setzen
        foreach (var col in PatientsGrid.Columns)
            col.SortDirection = null;

        e.Column.SortDirection = _sortDir;

        _listManager.SetSorting(_sortBy, _sortDir);
        _listManager.GoToFirstPage();
        RenderList();
    }

    // ------------------------
    // CRUD Buttons
    // ------------------------

    private async void Refresh_Click(object sender, RoutedEventArgs e)
        => await LoadPatientsAsync();

    private async void AddPatient_Click(object sender, RoutedEventArgs e)
    {
        if (await _crud.AddAsync(this))
            await LoadPatientsAsync();
    }

    private async void EditPatient_Click(object sender, RoutedEventArgs e)
    {
        if (PatientsGrid.SelectedItem is not Patient selected)
        {
            MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
            return;
        }

        if (await _crud.EditAsync(this, selected))
            await LoadPatientsAsync();
    }

    private async void DeletePatient_Click(object sender, RoutedEventArgs e)
    {
        if (PatientsGrid.SelectedItem is not Patient selected)
        {
            MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
            return;
        }

        if (await _crud.DeleteAsync(selected))
            await LoadPatientsAsync();
    }

    private async void ToggleActive_Click(object sender, RoutedEventArgs e)
    {
        if (PatientsGrid.SelectedItem is not Patient selected)
        {
            MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
            return;
        }

        if (await _crud.ToggleActiveAsync(selected)) ;
        await LoadPatientsAsync();
    }

    // ------------------------
    // CSV Export
    // ------------------------

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_listManager == null) return;

        // Exportiert die GEFILTERTE Liste (nicht nur Seite)
        var exportList = _listManager.GetFilteredAndSorted(CurrentSearchTerm, OnlyActive);

        if (exportList.Count == 0)
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

        foreach (var p in exportList)
        {
            var status = p.IsActive ? "Aktiv" : "Inaktiv";
            sb.AppendLine($"{p.Id};{Clean(p.Nachname)};{Clean(p.Vorname)};{p.Geburtsdatum:yyyy-MM-dd};{p.Alter};{Clean(p.Email)};{Clean(p.Telefonnummer)};{status}");
        }

        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show("Export erfolgreich ✅", "CSV Export", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ------------------------
    // Detail + Shortcuts
    // ------------------------

    private void PatientsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (PatientsGrid.SelectedItem is not Patient selected) return;
        var wnd = new PatientDetailWindow(selected) { Owner = this };
        wnd.ShowDialog();
    }

    private void PatientsGrid_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            DeletePatient_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
        {
            AddPatient_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
        {
            ExportCsv_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            Refresh_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }
    // Termin legen fenster öffnet
    private void OpenAddAppointmentWindow_Click(object sender, RoutedEventArgs e)
    {
        var window = App.ServiceProvider.GetRequiredService<AddAppointmentWindow>();
        window.Owner = this;
        window.ShowDialog();
    }
    //Termin list fenster anzeigen
    private void OpenAppointments_Click(object sender, RoutedEventArgs e)
    {
        var window = App.ServiceProvider.GetRequiredService<AppointmentWindow>();
        window.Owner = this;
        window.ShowDialog();
    }
}