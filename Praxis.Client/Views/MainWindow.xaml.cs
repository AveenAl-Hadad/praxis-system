using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Praxis.Client.Session;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

public partial class MainWindow : Window
{
    private readonly IPatientService _patientService;
    private List<Patient> _allPatients = new();
    private List<Patient> _filteredPatients = new();
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDashboardService _dashboardService;

    private int _currentPage = 1;
    private const int PageSize = 20;

    public MainWindow(IPatientService patientService,
                      IAuthService authService,
                      IServiceProvider serviceProvider,
                      IDashboardService dashboardService)
    {
        InitializeComponent();
        _patientService = patientService;
        _authService = authService;
        _serviceProvider = serviceProvider;
        _dashboardService = dashboardService;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (!UserSession.IsLoggedIn)
        {
            MessageBox.Show("Bitte zuerst anmelden.");
            Close();
            return;
        }

        LoggedInUserText.Text = $"Angemeldet: {UserSession.CurrentUser?.Username} ({UserSession.CurrentUser?.Role})";
     //   UserManagementButton.IsEnabled = UserSession.HasRole(Roles.Administrator) || UserSession.HasRole(Roles.Arzt);
        
        ApplyRolePermissions();
        await LoadPatientsAsync();
        await LoadDashboardAsync();

    }


    private async Task LoadPatientsAsync()
    {
        try
        {
            var patients = await _patientService.GetAllPatientsAsync();
            _allPatients = patients.ToList();
            _currentPage = 1;
            ApplyFilterAndPaging();
            StatusText.Text = $"Patienten geladen: {_allPatients.Count}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Fehler beim Laden der Patienten.";
            MessageBox.Show(
                $"Fehler beim Laden der Patienten:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    public async Task LoadDashboardAsync()
    {
        var stats = await _dashboardService.GetStatsAsync();

        TotalPatientsText.Text = stats.TotalPatients.ToString();
        TotalAppointmentsText.Text = stats.TotalAppointments.ToString();
        TotalInvoicesText.Text = stats.TotalInvoices.ToString();
        TotalPrescriptionsText.Text = stats.TotalPrescriptions.ToString();
        TotalRevenueText.Text = $"{stats.TotalRevenue:N2} €";

        CurrentMonthAppointmentsText.Text = stats.CurrentMonthAppointments.ToString();
        CurrentMonthInvoicesText.Text = stats.CurrentMonthInvoices.ToString();
        CurrentMonthRevenueText.Text = $"{stats.CurrentMonthRevenue:N2} €";

        UpdateChart(stats);
    }
    private void ApplyRolePermissions()
    {
        bool canManagePatients = UserSession.HasAnyRole(
            Roles.Administrator,
            Roles.Arzt,
            Roles.Mitarbeiter);

        bool canManageAppointments = UserSession.HasAnyRole(
            Roles.Administrator,
            Roles.Arzt,
            Roles.Mitarbeiter);

        bool canManageInvoices = UserSession.HasAnyRole(
            Roles.Administrator,
            Roles.Mitarbeiter);

        bool canManagePrescriptions = UserSession.HasAnyRole(
            Roles.Administrator,
            Roles.Arzt);

        bool canManageDocuments = UserSession.HasAnyRole(
            Roles.Administrator,
            Roles.Arzt,
            Roles.Mitarbeiter);

        bool canManageUsers = UserSession.HasRole(Roles.Administrator);

        AddPatientButton.IsEnabled = canManagePatients;
        EditPatientButton.IsEnabled = canManagePatients;
        DeletePatientButton.IsEnabled = canManagePatients;
        ToggleActiveButton.IsEnabled = canManagePatients;
        ExportCsvButton.IsEnabled = canManagePatients;

        AddAppointmentButton.IsEnabled = canManageAppointments;
        AppointmentsNavigationButton.IsEnabled = canManageAppointments;
        CalendarButton.IsEnabled = canManageAppointments;
        WaitingRoomButton.IsEnabled = canManageAppointments;

        InvoiceButton.IsEnabled = canManageInvoices;
        PrescriptionButton.IsEnabled = canManagePrescriptions;
        DocumentsButton.IsEnabled = canManageDocuments;

        UserManagementButton.IsEnabled = canManageUsers;
    }
    private void UpdateChart(DashboardStats stats)
    {
        var maxValue = new[]
                            {
                            stats.CurrentMonthAppointments,
                            stats.CurrentMonthInvoices,
                            (int)Math.Ceiling(stats.CurrentMonthRevenue)
                            }.Max();

        if (maxValue <= 0)
            maxValue = 1;

        AppointmentsChartBar.Maximum = maxValue;
        InvoicesChartBar.Maximum = maxValue;
        RevenueChartBar.Maximum = maxValue;

        AppointmentsChartBar.Value = stats.CurrentMonthAppointments;
        InvoicesChartBar.Value = stats.CurrentMonthInvoices;
        RevenueChartBar.Value = (double)stats.CurrentMonthRevenue;

        AppointmentsChartLabel.Text = $"{stats.CurrentMonthAppointments} Termine";
        InvoicesChartLabel.Text = $"{stats.CurrentMonthInvoices} Rechnungen";
        RevenueChartLabel.Text = $"{stats.CurrentMonthRevenue:N2} € Umsatz";
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
                (!string.IsNullOrWhiteSpace(p.Vorname) && p.Vorname.ToLower().Contains(searchText)) ||
                (!string.IsNullOrWhiteSpace(p.Nachname) && p.Nachname.ToLower().Contains(searchText)) ||
                (!string.IsNullOrWhiteSpace(p.Email) && p.Email.ToLower().Contains(searchText)) ||
                (!string.IsNullOrWhiteSpace(p.Telefonnummer) && p.Telefonnummer.ToLower().Contains(searchText)));
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

        PatientsGrid.ItemsSource = null;
        PatientsGrid.ItemsSource = pageItems;
        PatientsGrid.Items.Refresh();

        PageInfoText.Text = $"Seite {_currentPage} / {totalPages}";
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
   
    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadPatientsAsync();
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
    //CRUD
    private async void AddPatient_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new AddPatientWindow
            {
                Owner = this
            };

            if (window.ShowDialog() == true && window.CreatedPatient != null)
            {
                await _patientService.AddPatientAsync(window.CreatedPatient);
                await LoadPatientsAsync();
                await LoadDashboardAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim Anlegen:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void EditPatient_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedPatient = GetSelectedPatient();

            if (selectedPatient == null)
            {
                MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
                return;
            }

            var copy = new Patient
            {
                Id = selectedPatient.Id,
                Vorname = selectedPatient.Vorname,
                Nachname = selectedPatient.Nachname,
                Geburtsdatum = selectedPatient.Geburtsdatum,
                Email = selectedPatient.Email,
                Telefonnummer = selectedPatient.Telefonnummer,
                IsActive = selectedPatient.IsActive
            };

            var window = new AddPatientWindow(copy)
            {
                Owner = this
            };

            if (window.ShowDialog() == true && window.CreatedPatient != null)
            {
                await _patientService.UpdatePatientAsync(window.CreatedPatient);
                await LoadPatientsAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim Bearbeiten:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void DeletePatient_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedPatient = GetSelectedPatient();

            if (selectedPatient == null)
            {
                MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
                return;
            }

            var result = MessageBox.Show(
                $"Patient {selectedPatient.Vorname} {selectedPatient.Nachname} wirklich löschen?",
                "Löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            await _patientService.DeletePatientAsync(selectedPatient.Id);
            await LoadPatientsAsync();
            //await LoadDashboardAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim Löschen:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void ToggleActive_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedPatient = GetSelectedPatient();

            if (selectedPatient == null)
            {
                MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
                return;
            }

            selectedPatient.IsActive = !selectedPatient.IsActive;
            await _patientService.UpdatePatientAsync(selectedPatient);
            await LoadPatientsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim Umschalten:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    //EXPORT
    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var exportList = _filteredPatients.Any() ? _filteredPatients : _allPatients;

            if (!exportList.Any())
            {
                MessageBox.Show("Keine Patienten vorhanden.");
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Title = "Patienten als CSV speichern",
                Filter = "CSV-Datei (*.csv)|*.csv",
                FileName = $"Patienten_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveFileDialog.ShowDialog() != true)
                return;

            static string Escape(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return "";

                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }

            var sb = new StringBuilder();
            sb.AppendLine("Id;Vorname;Nachname;Geburtsdatum;Email;Telefonnummer;IstAktiv");

            foreach (var patient in exportList)
            {
                sb.AppendLine(
                    $"{patient.Id};" +
                    $"{Escape(patient.Vorname)};" +
                    $"{Escape(patient.Nachname)};" +
                    $"{patient.Geburtsdatum:yyyy-MM-dd};" +
                    $"{Escape(patient.Email)};" +
                    $"{Escape(patient.Telefonnummer)};" +
                    $"{patient.IsActive}");
            }

            File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);

            MessageBox.Show("CSV wurde erfolgreich exportiert.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim CSV-Export:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void PatientsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        EditPatient_Click(sender, e);
    }

    private void PatientsGrid_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
            DeletePatient_Click(sender, e);
    }

    private void PatientsGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;

        Func<Patient, object> keySelector = e.Column.SortMemberPath switch
        {
            nameof(Patient.Id) => p => p.Id,
            nameof(Patient.Nachname) => p => p.Nachname ?? "",
            nameof(Patient.Vorname) => p => p.Vorname ?? "",
            nameof(Patient.Geburtsdatum) => p => p.Geburtsdatum,
            nameof(Patient.Alter) => p => p.Alter,
            nameof(Patient.Email) => p => p.Email ?? "",
            nameof(Patient.Telefonnummer) => p => p.Telefonnummer ?? "",
            nameof(Patient.IsActive) => p => p.IsActive,
            _ => p => p.Id
        };

        bool ascending = e.Column.SortDirection != System.ComponentModel.ListSortDirection.Ascending;

        _allPatients = ascending
            ? _allPatients.OrderBy(keySelector).ToList()
            : _allPatients.OrderByDescending(keySelector).ToList();

        e.Column.SortDirection = ascending
            ? System.ComponentModel.ListSortDirection.Ascending
            : System.ComponentModel.ListSortDirection.Descending;

        ApplyFilterAndPaging();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
            Refresh_Click(sender, e);

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
            AddPatient_Click(sender, e);

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
            ExportCsv_Click(sender, e);
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

    private void UserManagementButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!UserSession.HasRole(Roles.Administrator))
            {
                MessageBox.Show("Nur Administratoren dürfen die Benutzerverwaltung öffnen.");
                return;
            }

            var window = App.ServiceProvider.GetRequiredService<UserManagementWindow>();
            window.Owner = this;
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim Öffnen der Benutzerverwaltung:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    //Logout-Methode
    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Möchten Sie sich wirklich abmelden?",
            "Abmelden",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        UserSession.Logout();

        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        Hide();

        var loginWindow = App.ServiceProvider.GetRequiredService<LoginWindow>();
        var loginResult = loginWindow.ShowDialog();

        if (loginResult == true)
        {
            var newMainWindow = App.ServiceProvider.GetRequiredService<MainWindow>();
            Application.Current.MainWindow = newMainWindow;
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            newMainWindow.Show();
            Close();
        }
        else
        {
            Application.Current.Shutdown();
        }
    }
    /// <summary>
    /// Passwort änderen
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (UserSession.CurrentUser == null)
            {
                MessageBox.Show("Kein Benutzer ist angemeldet.");
                return;
            }

            var window = new ChangePasswordWindow
            {
                Owner = this
            };

            if (window.ShowDialog() != true ||
                string.IsNullOrWhiteSpace(window.OldPassword) ||
                string.IsNullOrWhiteSpace(window.NewPassword))
            {
                return;
            }

            await _authService.ChangePasswordAsync(
                UserSession.CurrentUser.Id,
                window.OldPassword,
                window.NewPassword);

            MessageBox.Show("Passwort wurde erfolgreich geändert.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim Ändern des Passworts:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    private void OpenInvoices_Click(object sender, RoutedEventArgs e)
    {
        var window = (InvoiceWindow)_serviceProvider.GetRequiredService(typeof(InvoiceWindow));
        window.Owner = this;
        window.ShowDialog();
    }
    private void OpenPrescriptions_Click(object sender, RoutedEventArgs e)
    {
        var window = _serviceProvider.GetRequiredService<PrescriptionWindow>();
        window.Owner = this;
        window.ShowDialog();
    }
    private void OpenDocuments_Click(object sender, RoutedEventArgs e)
    {
        if (PatientsGrid.SelectedItem is not Patient patient)
        {
            MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
            return;
        }
            
        var window = ActivatorUtilities.CreateInstance<DocumentWindow>(_serviceProvider, patient);
        window.Owner = this;
        window.ShowDialog();
    }
    public async void RefreshDashboard_Click(object sender, RoutedEventArgs e)
    {
        await LoadDashboardAsync();
    }

    private void PatientsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }
}