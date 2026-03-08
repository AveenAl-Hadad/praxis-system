using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Client.Session;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services.Interface;

namespace Praxis.Client.Views;

public partial class MainWindow : Window
{
    private readonly IPatientService _patientService;
    private List<Patient> _allPatients = new();
    private List<Patient> _filteredPatients = new();

    private int _currentPage = 1;
    private const int PageSize = 10;

    public MainWindow(IPatientService patientService)
    {
        InitializeComponent();
        _patientService = patientService;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (!UserSession.IsLoggedIn)
        {
            MessageBox.Show("Bitte zuerst anmelden.");
            Close();
            return;
        }

        if (!UserSession.HasRole(Roles.Administrator))
        {
            UserManagementButton.IsEnabled = false;
        }

        await LoadPatientsAsync();
    }

    private async System.Threading.Tasks.Task LoadPatientsAsync()
    {
        try
        {
            var patients = await _patientService.GetAllPatientsAsync();
            _allPatients = patients.ToList();
            ApplyFilterAndPaging();
            StatusText.Text = $"Patienten geladen: {_allPatients.Count}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Fehler beim Laden der Patienten.";
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilterAndPaging()
    {
        if (PatientsGrid == null || PageInfoText == null || SearchBox == null || OnlyActiveCheck == null)
            return;

        IEnumerable<Patient> query = _allPatients;

        var searchText = SearchBox.Text?.Trim().ToLower() ?? string.Empty;
        var onlyActive = OnlyActiveCheck.IsChecked == true;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(p =>
                (p.Vorname != null && p.Vorname.ToLower().Contains(searchText)) ||
                (p.Nachname != null && p.Nachname.ToLower().Contains(searchText)) ||
                (p.Email != null && p.Email.ToLower().Contains(searchText)));
        }

        if (onlyActive)
        {
            query = query.Where(p => p.IsActive);
        }

        _filteredPatients = query.ToList();

        var totalPages = Math.Max(1, (int)Math.Ceiling((double)_filteredPatients.Count / PageSize));

        if (_currentPage > totalPages)
            _currentPage = totalPages;

        if (_currentPage < 1)
            _currentPage = 1;

        var pageItems = _filteredPatients
            .Skip((_currentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        PatientsGrid.ItemsSource = pageItems;
        PageInfoText.Text = $"Seite {_currentPage} / {totalPages}";
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadPatientsAsync();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _currentPage = 1;
        ApplyFilterAndPaging();
    }

    private void OnlyActiveCheck_Changed(object sender, RoutedEventArgs e)
    {
        _currentPage = 1;
        ApplyFilterAndPaging();
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            ApplyFilterAndPaging();
        }
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)_filteredPatients.Count / PageSize));

        if (_currentPage < totalPages)
        {
            _currentPage++;
            ApplyFilterAndPaging();
        }
    }

    private Patient? GetSelectedPatient()
    {
        return PatientsGrid.SelectedItem as Patient;
    }

    private void AddPatient_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Patient anlegen noch nicht verbunden.");
    }

    private void EditPatient_Click(object sender, RoutedEventArgs e)
    {
        var selectedPatient = GetSelectedPatient();

        if (selectedPatient == null)
        {
            MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
            return;
        }

        MessageBox.Show($"Patient bearbeiten: {selectedPatient.Vorname} {selectedPatient.Nachname}");
    }

    private void DeletePatient_Click(object sender, RoutedEventArgs e)
    {
        var selectedPatient = GetSelectedPatient();

        if (selectedPatient == null)
        {
            MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
            return;
        }

        MessageBox.Show($"Patient löschen: {selectedPatient.Vorname} {selectedPatient.Nachname}");
    }

    private void ToggleActive_Click(object sender, RoutedEventArgs e)
    {
        var selectedPatient = GetSelectedPatient();

        if (selectedPatient == null)
        {
            MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
            return;
        }

        MessageBox.Show($"Aktiv/Inaktiv umschalten für: {selectedPatient.Vorname} {selectedPatient.Nachname}");
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("CSV Export noch nicht verbunden.");
    }

    private void PatientsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        var selectedPatient = GetSelectedPatient();

        if (selectedPatient != null)
        {
            MessageBox.Show($"Doppelklick auf: {selectedPatient.Vorname} {selectedPatient.Nachname}");
        }
    }

    private void PatientsGrid_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            DeletePatient_Click(sender, e);
        }
    }

    private void PatientsGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        // Optional: eigene Sortierung später ergänzen
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            Refresh_Click(sender, e);
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
        {
            AddPatient_Click(sender, e);
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
        {
            ExportCsv_Click(sender, e);
        }
    }

    private void OpenAddAppointmentWindow_Click(object sender, RoutedEventArgs e)
    {
        var window = App.ServiceProvider.GetRequiredService<AddAppointmentWindow>();
        window.Owner = this;
        window.ShowDialog();
    }

    private void OpenAppointments_Click(object sender, RoutedEventArgs e)
    {
        var window = App.ServiceProvider.GetRequiredService<AppointmentWindow>();
        window.Owner = this;
        window.ShowDialog();
    }

    private void OpenCalendar_Click(object sender, RoutedEventArgs e)
    {
        var window = App.ServiceProvider.GetRequiredService<AppointmentCalendarWindow>();
        window.Owner = this;
        window.ShowDialog();
    }

    private void OpenWaitingRoom_Click(object sender, RoutedEventArgs e)
    {
        var window = App.ServiceProvider.GetRequiredService<WaitingRoomWindow>();
        window.Owner = this;
        window.ShowDialog();
    }
}