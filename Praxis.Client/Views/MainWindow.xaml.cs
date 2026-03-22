using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Praxis.Client.Logic.UI;
using Praxis.Client.Session;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using Praxis.Infrastructure.Services.Interface;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Praxis.Client.Views;

/// <summary>
/// Das Hauptfenster der Anwendung.
/// 
/// In diesem Fenster werden die wichtigsten Funktionen der Praxissoftware gesteuert:
/// - Patienten anzeigen, suchen, filtern und bearbeiten
/// - Dashboard-Daten laden
/// - Termine, Rechnungen, Rezepte und Dokumente öffnen
/// - Benutzerverwaltung, Audit-Log, Backup und Theme wechseln
/// - Abmelden und Passwort ändern
/// 
/// Die MainWindow-Klasse ist damit die zentrale Benutzeroberfläche der Anwendung.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Service für Patientenverwaltung.
    /// </summary>
    private readonly IPatientService _patientService;

    /// <summary>
    /// Service für Terminverwaltung.
    /// </summary>
    private readonly IAppointmentService _appointmentService;

    /// <summary>
    /// Service für Authentifizierung und Passwortänderung.
    /// </summary>
    private readonly IAuthService _authService;

    /// <summary>
    /// Allgemeiner ServiceProvider, um Fenster und Services dynamisch zu laden.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Service für Dashboard-Daten.
    /// </summary>
    private readonly IDashboardService _dashboardService;

    /// <summary>
    /// Service zum Erstellen und Wiederherstellen von Backups.
    /// </summary>
    private readonly IBackupService _backupService;

    /// <summary>
    /// Service für Audit-Logs.
    /// </summary>
    private readonly IAuditService _auditService;

    /// <summary>
    /// Service zum Umschalten zwischen Hell- und Dunkelmodus.
    /// </summary>
    private readonly IThemeService _themeService;

    /// <summary>
    /// Enthält alle geladenen Patienten.
    /// </summary>
    private List<Patient> _allPatients = new();

    /// <summary>
    /// Enthält nur die Patienten, die nach Suche und Filter übrig bleiben.
    /// </summary>
    private List<Patient> _filteredPatients = new();

    /// <summary>
    /// Speichert die aktuell angezeigte Seite bei der Seitennavigation.
    /// </summary>
    private int _currentPage = 1;

    /// <summary>
    /// Anzahl der Patienten pro Seite.
    /// </summary>
    private const int PageSize = 10;

    /// <summary>
    /// Konstruktor des Hauptfensters.
    /// 
    /// Übergibt alle benötigten Services und speichert sie in Feldern,
    /// damit sie in den Methoden des Fensters verwendet werden können.
    /// </summary>
    public MainWindow(
        IPatientService patientService,
        IAppointmentService appointmentService,
        IAuthService authService,
        IServiceProvider serviceProvider,
        IDashboardService dashboardService,
        IBackupService backupService,
        IAuditService auditService,
        IThemeService themeService)
    {
        InitializeComponent();
        _patientService = patientService;
        _appointmentService = appointmentService;
        _authService = authService;
        _serviceProvider = serviceProvider;
        _dashboardService = dashboardService;
        _backupService = backupService;
        _auditService = auditService;
        _themeService = themeService;
    }

    /// <summary>
    /// Wird beim Laden des Fensters ausgeführt.
    /// 
    /// Es wird geprüft, ob ein Benutzer angemeldet ist.
    /// Danach werden:
    /// - der angemeldete Benutzer angezeigt
    /// - Rollenrechte gesetzt
    /// - Patienten geladen
    /// - Dashboard-Daten geladen
    /// </summary>
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (!UserSession.IsLoggedIn)
        {
            MessageBox.Show("Bitte zuerst anmelden.");
            Close();
            return;
        }

        LoggedInUserText.Text = $"Angemeldet: {UserSession.CurrentUser?.Username} ({UserSession.CurrentUser?.Role})";

        ApplyRolePermissions();
        await LoadPatientsAsync();
        await LoadDashboardAsync();
    }

    /// <summary>
    /// Lädt die Kennzahlen für das Dashboard neu.
    /// 
    /// Angezeigt werden:
    /// - Gesamtzahl aller Patienten
    /// - Anzahl aktiver Patienten
    /// - Anzahl der heutigen Termine
    /// </summary>
    public async Task LoadDashboardAsync()
    {
        var patients = await _patientService.GetAllPatientsAsync();
        TotalPatientsText.Text = patients.Count().ToString();
        ActivePatientsText.Text = patients.Count(p => p.IsActive).ToString();

        var today = DateTime.Today;
        var appointments = await _appointmentService.GetAllAppointmentsAsync();
        TodayAppointmentsText.Text = appointments.Count(a => a.StartTime.Date == today).ToString();
    }

    /// <summary>
    /// Lädt alle Patienten aus der Datenbank.
    /// 
    /// Danach wird die erste Seite angezeigt
    /// und Suche, Filter sowie Paging werden angewendet.
    /// </summary>
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

    /// <summary>
    /// Aktiviert oder deaktiviert Buttons abhängig von der Rolle
    /// des aktuell angemeldeten Benutzers.
    /// 
    /// So wird sichergestellt, dass nur berechtigte Benutzer
    /// bestimmte Bereiche der Anwendung verwenden können.
    /// </summary>
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

        bool canManageBackup = UserSession.HasRole(Roles.Administrator);
        bool canManageUsers = UserSession.HasRole(Roles.Administrator);

        BackupButton.IsEnabled = canManageBackup;
        RestoreButton.IsEnabled = canManageBackup;
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

    /// <summary>
    /// Führt Suche, Aktiv-Filter und Seitennavigation auf der Patientenliste aus.
    /// 
    /// Ablauf:
    /// - Alle Patienten werden als Ausgangsbasis verwendet
    /// - Suchtext wird angewendet
    /// - optional nur aktive Patienten anzeigen
    /// - Daten werden in Seiten aufgeteilt
    /// - aktuelle Seite wird im DataGrid angezeigt
    /// </summary>
    private void ApplyFilterAndPaging()
    {
        if (PatientsGrid == null || PageInfoText == null || SearchBox == null || OnlyActiveCheck == null)
            return;

        IEnumerable<Patient> query = _allPatients;

        var searchText = SearchBox.Text?.Trim().ToLower() ?? string.Empty;
        var onlyActive = OnlyActiveCheck.IsChecked == true;

        // Suche nach Vorname, Nachname, E-Mail oder Telefonnummer
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(p =>
                (!string.IsNullOrWhiteSpace(p.Vorname) && p.Vorname.ToLower().Contains(searchText)) ||
                (!string.IsNullOrWhiteSpace(p.Nachname) && p.Nachname.ToLower().Contains(searchText)) ||
                (!string.IsNullOrWhiteSpace(p.Email) && p.Email.ToLower().Contains(searchText)) ||
                (!string.IsNullOrWhiteSpace(p.Telefonnummer) && p.Telefonnummer.ToLower().Contains(searchText)));
        }

        // Optional nur aktive Patienten anzeigen
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

        // Nur die Datensätze der aktuellen Seite anzeigen
        var pageItems = _filteredPatients
            .Skip((_currentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        PatientsGrid.ItemsSource = null;
        PatientsGrid.ItemsSource = pageItems;
        PatientsGrid.Items.Refresh();

        PageInfoText.Text = $"Seite {_currentPage} / {totalPages}";
    }

    /// <summary>
    /// Wird ausgeführt, wenn sich der Suchtext ändert.
    /// Setzt die Seite auf 1 zurück und filtert die Patienten neu.
    /// </summary>
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _currentPage = 1;
        ApplyFilterAndPaging();
    }

    /// <summary>
    /// Wird ausgeführt, wenn die Checkbox "Nur aktive Patienten"
    /// geändert wird.
    /// </summary>
    private void OnlyActiveCheck_Changed(object sender, RoutedEventArgs e)
    {
        _currentPage = 1;
        ApplyFilterAndPaging();
    }

    /// <summary>
    /// Lädt Patientenliste und Dashboard neu.
    /// </summary>
    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadPatientsAsync();
        await LoadDashboardAsync();
    }

    /// <summary>
    /// Blättert eine Seite zurück.
    /// </summary>
    private void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            ApplyFilterAndPaging();
        }
    }

    /// <summary>
    /// Blättert eine Seite weiter.
    /// </summary>
    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)_filteredPatients.Count / PageSize));

        if (_currentPage < totalPages)
        {
            _currentPage++;
            ApplyFilterAndPaging();
        }
    }

    /// <summary>
    /// Gibt den aktuell im DataGrid ausgewählten Patienten zurück.
    /// </summary>
    private Patient? GetSelectedPatient()
    {
        return PatientsGrid.SelectedItem as Patient;
    }

    /// <summary>
    /// Öffnet das Fenster zum Erstellen eines neuen Patienten.
    /// 
    /// Wenn der Benutzer speichert, wird der Patient in die Datenbank übernommen
    /// und danach Liste und Dashboard aktualisiert.
    /// </summary>
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
                await _patientService.AddPatientAsync(window.CreatedPatient, UserSession.CurrentUser?.Username ?? "System");
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

    /// <summary>
    /// Öffnet das Bearbeitungsfenster für den ausgewählten Patienten.
    /// 
    /// Vor dem Bearbeiten wird eine Kopie des Patienten erstellt,
    /// damit Änderungen nicht sofort das Originalobjekt beeinflussen.
    /// </summary>
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
                await LoadDashboardAsync();
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

    /// <summary>
    /// Löscht den ausgewählten Patienten nach Rückfrage.
    /// </summary>
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

            await _patientService.DeletePatientAsync(selectedPatient.Id, UserSession.CurrentUser?.Username ?? "System");
            await LoadPatientsAsync();
            await LoadDashboardAsync();
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

    /// <summary>
    /// Aktiviert oder deaktiviert den ausgewählten Patienten.
    /// 
    /// Dabei wird der Wert von IsActive umgeschaltet.
    /// </summary>
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
            await LoadDashboardAsync();
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

    /// <summary>
    /// Exportiert die aktuell gefilterte Patientenliste als CSV-Datei.
    /// 
    /// Wenn kein Filter aktiv ist, werden alle Patienten exportiert.
    /// </summary>
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

            // Hilfsmethode zum Absichern von Textwerten in CSV-Dateien
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

    /// <summary>
    /// Öffnet bei Doppelklick auf einen Patienten direkt das Bearbeitungsfenster.
    /// </summary>
    private void PatientsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        EditPatient_Click(sender, e);
    }

    /// <summary>
    /// Löscht bei Drücken der Entf-Taste den ausgewählten Patienten.
    /// </summary>
    private void PatientsGrid_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
            DeletePatient_Click(sender, e);
    }

    /// <summary>
    /// Führt eine benutzerdefinierte Sortierung der Patientenliste aus,
    /// abhängig von der angeklickten Spalte im DataGrid.
    /// </summary>
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

    /// <summary>
    /// Unterstützt Tastenkürzel im Hauptfenster:
    /// - F5 = Aktualisieren
    /// - Strg + N = Neuer Patient
    /// - Strg + E = CSV-Export
    /// </summary>
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
            Refresh_Click(sender, e);

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
            AddPatient_Click(sender, e);

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
            ExportCsv_Click(sender, e);
    }

    /// <summary>
    /// Öffnet das Fenster zum Erstellen eines neuen Termins.
    /// </summary>
    private void OpenAddAppointmentWindow_Click(object sender, RoutedEventArgs e)
    {
        var window = App.ServiceProvider.GetRequiredService<AddAppointmentWindow>();
        window.Owner = this;
        window.ShowDialog();
    }

    /// <summary>
    /// Öffnet die Terminübersicht.
    /// </summary>
    private void OpenAppointments_Click(object sender, RoutedEventArgs e)
    {
        var window = App.ServiceProvider.GetRequiredService<AppointmentWindow>();
        window.Owner = this;
        window.ShowDialog();
    }

    /// <summary>
    /// Öffnet die Kalenderansicht der Termine.
    /// </summary>
    private void OpenCalendar_Click(object sender, RoutedEventArgs e)
    {
        var window = App.ServiceProvider.GetRequiredService<AppointmentCalendarWindow>();
        window.Owner = this;
        window.ShowDialog();
    }

    /// <summary>
    /// Öffnet das Wartezimmer-Fenster.
    /// </summary>
    private void OpenWaitingRoom_Click(object sender, RoutedEventArgs e)
    {
        var window = App.ServiceProvider.GetRequiredService<WaitingRoomWindow>();
        window.Owner = this;
        window.ShowDialog();
    }

    /// <summary>
    /// Öffnet die Benutzerverwaltung.
    /// 
    /// Nur Administratoren dürfen diesen Bereich aufrufen.
    /// </summary>
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

    /// <summary>
    /// Meldet den aktuellen Benutzer ab.
    /// 
    /// Danach wird erneut das Login-Fenster geöffnet.
    /// Bei erfolgreichem Login wird ein neues Hauptfenster gestartet.
    /// </summary>
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
    /// Öffnet das Fenster zum Ändern des Passworts
    /// und speichert das neue Passwort über den AuthService.
    /// </summary>
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

    /// <summary>
    /// Öffnet das Rechnungsfenster.
    /// </summary>
    private void OpenInvoices_Click(object sender, RoutedEventArgs e)
    {
        var window = (InvoiceWindow)_serviceProvider.GetRequiredService(typeof(InvoiceWindow));
        window.Owner = this;
        window.ShowDialog();
    }

    /// <summary>
    /// Öffnet das Rezeptfenster.
    /// </summary>
    private void OpenPrescriptions_Click(object sender, RoutedEventArgs e)
    {
        var window = _serviceProvider.GetRequiredService<PrescriptionWindow>();
        window.Owner = this;
        window.ShowDialog();
    }

    /// <summary>
    /// Öffnet das Dokumentenfenster für den aktuell ausgewählten Patienten.
    /// </summary>
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

    /// <summary>
    /// Aktualisiert nur das Dashboard.
    /// </summary>
    public async void RefreshDashboard_Click(object sender, RoutedEventArgs e)
    {
        await LoadDashboardAsync();
    }

    /// <summary>
    /// Event für Auswahländerung im Patienten-Grid.
    /// Aktuell ohne Logik.
    /// </summary>
    private void PatientsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    /// <summary>
    /// Öffnet das Audit-Log-Fenster.
    /// </summary>
    private void OpenAuditLog_Click(object sender, RoutedEventArgs e)
    {
        var window = _serviceProvider.GetRequiredService<AuditLogWindow>();
        window.Owner = this;
        window.ShowDialog();
    }

    /// <summary>
    /// Erstellt ein Datenbank-Backup im Dokumente-Ordner des Benutzers.
    /// 
    /// Nur Administratoren dürfen ein Backup erstellen.
    /// Nach erfolgreichem Backup wird zusätzlich ein Audit-Log-Eintrag geschrieben.
    /// </summary>
    private async void CreateBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!UserSession.HasRole(Roles.Administrator))
            {
                MessageBox.Show("Nur Administratoren dürfen ein Backup erstellen.");
                return;
            }

            var backupFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "PraxisBackups");

            Directory.CreateDirectory(backupFolder);

            var filePath = Path.Combine(
                backupFolder,
                $"praxis_backup_{DateTime.Now:yyyy-MM-dd_HHmmss}.db");

            await _backupService.CreateBackupAsync(filePath);
            await _auditService.LogAsync(
                UserSession.CurrentUser?.Username ?? "System",
                "BACKUP",
                "Database",
                $"Backup erstellt: {filePath}");

            MessageBox.Show($"Backup wurde erfolgreich erstellt:\n{filePath}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Backup:\n{ex.Message}");
        }
    }

    /// <summary>
    /// Stellt ein vorhandenes Backup wieder her.
    /// 
    /// Achtung:
    /// Dabei werden aktuelle Daten überschrieben.
    /// Nur Administratoren dürfen diese Funktion ausführen.
    /// </summary>
    private async void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!UserSession.HasRole(Roles.Administrator))
            {
                MessageBox.Show("Nur Administratoren dürfen ein Backup wiederherstellen.");
                return;
            }

            var result = MessageBox.Show(
                "ACHTUNG: Alle aktuellen Daten werden überschrieben!\n\nFortfahren?",
                "Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            var dialog = new OpenFileDialog
            {
                Filter = "SQLite Backup (*.db)|*.db|Alle Dateien (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            await _backupService.RestoreBackupAsync(dialog.FileName);

            MessageBox.Show("Backup wurde erfolgreich wiederhergestellt.\nDie Anwendung wird jetzt beendet.");
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Wiederherstellen:\n{ex.Message}");
        }
    }

    /// <summary>
    /// Wechselt zwischen Hell- und Dunkelmodus.
    /// 
    /// Zusätzlich wird das Symbol des Buttons angepasst:
    /// - 🌙 für Dunkelmodus aktivierbar
    /// - ☀ für Hellmodus aktivierbar
    /// </summary>
    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_themeService.IsDarkMode)
        {
            _themeService.ApplyLightTheme();
            ThemeToggleButton.Content = new TextBlock
            {
                Text = "🌙",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        else
        {
            _themeService.ApplyDarkTheme();
            ThemeToggleButton.Content = new TextBlock
            {
                Text = "☀",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
    }

    /// <summary>
    /// Öffnet das Fenster für Online-Terminbuchungen.
    /// 
    /// Wenn dort ein Termin erfolgreich erstellt wurde,
    /// wird das Dashboard neu geladen.
    /// </summary>
    private async void OnlineBookingButton_Click(object sender, RoutedEventArgs e)
    {
        var bookingWindow = App.ServiceProvider.GetRequiredService<OnlineBookingWindow>();
        bookingWindow.Owner = this;

        var result = bookingWindow.ShowDialog();

        if (result == true)
        {
            await LoadDashboardAsync();
        }
    }
}