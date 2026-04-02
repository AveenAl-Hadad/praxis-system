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
using Microsoft.VisualBasic;
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
    private readonly IDocumentService _documentService;
    private readonly IUserManagementService _userManagementService;

    private enum BottomModule
    {
        Patienten,
        Labor,
        Abrechnung,
        Auswertungen,
        Nachrichten,
        Kataloge,
        Einrichtung,
        Einstellungen
    }

    private BottomModule _currentModule = BottomModule.Patienten;

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
    private sealed class AppointmentTabRow
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = "";
        public string DateText { get; set; } = "";
        public string TimeText { get; set; } = "";
        public string DurationText { get; set; } = "";
        public string Reason { get; set; } = "";
        public string Status { get; set; } = "";
        public Appointment Source { get; set; } = null!;
    }

    private sealed class WaitingRoomTabRow
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = "";
        public string ArrivalTimeText { get; set; } = "";
        public string WaitingDurationText { get; set; } = "";
        public string Status { get; set; } = "";
        public string Room { get; set; } = "-";
        public Appointment Source { get; set; } = null!;
    }

    private sealed class DocumentTabRow
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string Category { get; set; } = "";
        public string DateText { get; set; } = "";
        public string FileName { get; set; } = "";
        public PatientDocument Source { get; set; } = null!;
    }

    private sealed class UserTabRow
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public User Source { get; set; } = null!;
    }

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
        IThemeService themeService,
        IUserManagementService userManagementService,
        IDocumentService documentService)
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
        _documentService = documentService;
        _userManagementService = userManagementService;
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
        await LoadAppointmentsTabAsync();
        await LoadWaitingRoomTabAsync();
        await LoadUsersTabAsync();
        await LoadDocumentsTabAsync();

        SwitchModule(BottomModule.Patienten);
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

    private async Task LoadAppointmentsTabAsync()
    {
        var appointments = await _appointmentService.GetAllAppointmentsAsync();

        var rows = appointments
            .OrderBy(a => a.StartTime)
            .Select(a => new AppointmentTabRow
            {
                Id = a.Id,
                PatientName = a.Patient == null
                    ? $"Patient #{a.PatientId}"
                    : $"{a.Patient.Vorname} {a.Patient.Nachname}".Trim(),
                DateText = a.StartTime.ToString("dd.MM.yyyy"),
                TimeText = a.StartTime.ToString("HH:mm"),
                DurationText = $"{a.DurationMinutes} Min.",
                Reason = a.Reason,
                Status = a.Status,
                Source = a
            })
            .ToList();

        AppointmentsTabGrid.ItemsSource = rows;

        TodayAppointmentsTabText.Text = rows.Count(r => r.Source.StartTime.Date == DateTime.Today).ToString();
        OpenAppointmentsTabText.Text = rows.Count(r => r.Status != "Erledigt" && r.Status != "Abgesagt").ToString();
        CompletedAppointmentsTabText.Text = rows.Count(r => r.Status == "Erledigt").ToString();

        AppointmentsTabStatusText.Text = $"Termine geladen: {rows.Count}";
    }

    private async Task LoadWaitingRoomTabAsync()
    {
        var appointments = await _appointmentService.GetWaitingRoomAppointmentsAsync(DateTime.Today);

        var now = DateTime.Now;

        var rows = appointments
            .OrderBy(a => a.StartTime)
            .Select(a => new WaitingRoomTabRow
            {
                Id = a.Id,
                PatientName = a.Patient == null
                    ? $"Patient #{a.PatientId}"
                    : $"{a.Patient.Vorname} {a.Patient.Nachname}".Trim(),
                ArrivalTimeText = a.StartTime.ToString("HH:mm"),
                WaitingDurationText = now > a.StartTime
                    ? $"{(int)(now - a.StartTime).TotalMinutes} Min."
                    : "0 Min.",
                Status = a.Status,
                Room = "-",
                Source = a
            })
            .ToList();

        WaitingRoomTabGrid.ItemsSource = rows;

        WaitingCountTabText.Text = rows.Count(r => r.Status == "Im Wartezimmer").ToString();
        CalledCountTabText.Text = rows.Count(r => r.Status == "In Behandlung").ToString();
        CompletedCountTabText.Text = rows.Count(r => r.Status == "Erledigt").ToString();

        WaitingRoomTabStatusText.Text = $"Wartezimmer geladen: {rows.Count}";
    }

    private async Task LoadUsersTabAsync()
    {
        var users = await _userManagementService.GetAllUsersAsync();

        var rows = users
            .OrderBy(u => u.Username)
            .Select(u => new UserTabRow
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                Source = u
            })
            .ToList();

        UsersTabGrid.ItemsSource = rows;

        TotalUsersTabText.Text = rows.Count.ToString();
        AdminUsersTabText.Text = rows.Count(r => r.Role == Roles.Administrator).ToString();
        ActiveUsersTabText.Text = rows.Count(r => r.IsActive).ToString();

        UsersTabStatusText.Text = $"Benutzer geladen: {rows.Count}";
    }

    private async Task LoadDocumentsTabAsync()
    {
        var selectedPatient = GetSelectedPatient();

        if (selectedPatient == null)
        {
            DocumentsTabGrid.ItemsSource = null;
            TotalDocumentsTabText.Text = "0";
            PdfDocumentsTabText.Text = "0";
            ChangedDocumentsTabText.Text = "0";
            DocumentsTabStatusText.Text = "Bitte links im Patienten-Tab einen Patienten auswählen.";
            return;
        }

        var documents = await _documentService.GetDocumentsByPatientAsync(selectedPatient.Id);

        var rows = documents
            .OrderByDescending(d => d.UploadDate)
            .Select(d => new DocumentTabRow
            {
                Id = d.Id,
                Title = Path.GetFileNameWithoutExtension(d.FileName),
                PatientName = $"{selectedPatient.Vorname} {selectedPatient.Nachname}".Trim(),
                Category = Path.GetExtension(d.FileName).Trim('.').ToUpper(),
                DateText = d.UploadDate.ToString("dd.MM.yyyy"),
                FileName = d.FileName,
                Source = d
            })
            .ToList();

        DocumentsTabGrid.ItemsSource = rows;

        TotalDocumentsTabText.Text = rows.Count.ToString();
        PdfDocumentsTabText.Text = rows.Count(r => r.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)).ToString();
        ChangedDocumentsTabText.Text = rows.Count(r => r.Source.UploadDate.Date == DateTime.Today).ToString();

        DocumentsTabStatusText.Text = $"Dokumente für {selectedPatient.Vorname} {selectedPatient.Nachname}: {rows.Count}";
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
        DocumentButton.IsEnabled = canManageDocuments;

        UserManagementButton.IsEnabled = canManageUsers;

        if (ToggleUserActiveButton != null)
            ToggleUserActiveButton.IsEnabled = canManageUsers;

        if (CreateUserTabButton != null)
            CreateUserTabButton.IsEnabled = canManageUsers;

        if (ChangeUserRoleTabButton != null)
            ChangeUserRoleTabButton.IsEnabled = canManageUsers;
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
        //var window = App.ServiceProvider.GetRequiredService<AppointmentWindow>();
        //window.Owner = this;
        //window.ShowDialog();
        SelectTab(1);
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
        //var window = App.ServiceProvider.GetRequiredService<WaitingRoomWindow>();
        //window.Owner = this;
        //window.ShowDialog();
        SelectTab(3);
    }

    /// <summary>
    /// Öffnet die Benutzerverwaltung.
    /// 
    /// Nur Administratoren dürfen diesen Bereich aufrufen.
    /// </summary>
    private void UserManagementButton_Click(object sender, RoutedEventArgs e)
    {
        //try
        //{
        //    if (!UserSession.HasRole(Roles.Administrator))
        //    {
        //        MessageBox.Show("Nur Administratoren dürfen die Benutzerverwaltung öffnen.");
        //        return;
        //    }

        //    var window = App.ServiceProvider.GetRequiredService<UserManagementWindow>();
        //    window.Owner = this;
        //    window.ShowDialog();
        //}
        //catch (Exception ex)
        //{
        //    MessageBox.Show(
        //        $"Fehler beim Öffnen der Benutzerverwaltung:\n{ex.Message}",
        //        "Fehler",
        //        MessageBoxButton.OK,
        //        MessageBoxImage.Error);
        //}
        SelectTab(4);
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
        //if (PatientsGrid.SelectedItem is not Patient patient)
        //{
        //    MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
        //    return;
        //}

        //var window = ActivatorUtilities.CreateInstance<DocumentWindow>(_serviceProvider, patient);
        //window.Owner = this;
        //window.ShowDialog();
        SelectTab(2);
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
        if (PatientsGrid.SelectedItem is Praxis.Domain.Entities.Patient patient)
        {
            SelectedPatientNameText.Text = $"{patient.Vorname} {patient.Nachname}";
            SelectedPatientBirthDateText.Text = patient.Geburtsdatum.ToString("dd.MM.yyyy");
            SelectedPatientPhoneText.Text = string.IsNullOrWhiteSpace(patient.Telefonnummer) ? "-" : patient.Telefonnummer;
            SelectedPatientEmailText.Text = string.IsNullOrWhiteSpace(patient.Email) ? "-" : patient.Email;
            SelectedPatientStatusText.Text = patient.IsActive ? "Aktiv" : "Inaktiv";
        }
        else
        {
            SelectedPatientNameText.Text = "Kein Patient gewählt";
            SelectedPatientBirthDateText.Text = "-";
            SelectedPatientPhoneText.Text = "-";
            SelectedPatientEmailText.Text = "-";
            SelectedPatientStatusText.Text = "-";
        }
        _ = LoadDocumentsTabAsync();
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
    private void BuildSidebar(BottomModule module)
    {
        if (DynamicSidebarPanel == null)
            return;

        DynamicSidebarPanel.Children.Clear();
        if (SidebarTitleText != null)
            SidebarTitleText.Text = module.ToString();

        switch (module)
        {
            case BottomModule.Patienten:
                AddSidebarButton("Patientensuche", ShowPatientsTab_Click);
                AddSidebarButton("Neuer Patient", AddPatient_Click);
                AddSidebarButton("Patient bearbeiten", EditPatient_Click);
                AddSidebarButton("Patient löschen", DeletePatient_Click);
                AddSidebarButton("Termine", OpenAppointments_Click);
                AddSidebarButton("Dokumente", OpenDocuments_Click);
                AddSidebarButton("Wartezimmer", OpenWaitingRoom_Click);
                break;

            case BottomModule.Labor:
                AddSidebarButton("Labordaten importieren", DummySidebarClick);
                AddSidebarButton("Laborberichte", DummySidebarClick);
                AddSidebarButton("Labortagesliste", DummySidebarClick);
                AddSidebarButton("Labore", DummySidebarClick);
                break;

            case BottomModule.Abrechnung:
                AddSidebarButton("Rechnungen öffnen", OpenInvoices_Click);
                AddSidebarButton("Neue Rechnung", OpenInvoices_Click);
                AddSidebarButton("Rezepte", OpenPrescriptions_Click);
                AddSidebarButton("Audit Log", OpenAuditLog_Click);
                break;

            case BottomModule.Auswertungen:
                AddSidebarButton("Dashboard", ShowPatientsTab_Click);
                AddSidebarButton("Terminkalender", OpenCalendar_Click);
                AddSidebarButton("Wartezimmerübersicht", OpenWaitingRoom_Click);
                AddSidebarButton("Patienten-Statistik", DummySidebarClick);
                break;

            case BottomModule.Nachrichten:
                AddSidebarButton("Online Buchung", OnlineBookingButton_Click);
                AddSidebarButton("Dokumente", OpenDocuments_Click);
                AddSidebarButton("Audit Log", OpenAuditLog_Click);
                break;

            case BottomModule.Kataloge:
                AddSidebarButton("Dokumente", OpenDocuments_Click);
                AddSidebarButton("Rezepte", OpenPrescriptions_Click);
                AddSidebarButton("Rechnungen", OpenInvoices_Click);
                break;

            case BottomModule.Einrichtung:
                AddSidebarButton("Benutzerverwaltung", UserManagementButton_Click);
                AddSidebarButton("Kalender", OpenCalendar_Click);
                AddSidebarButton("Online Buchung", OnlineBookingButton_Click);
                AddSidebarButton("Wartezimmer", OpenWaitingRoom_Click);
                break;

            case BottomModule.Einstellungen:
                AddSidebarButton("Design wechseln", ThemeToggle_Click);
                AddSidebarButton("Backup erstellen", CreateBackup_Click);
                AddSidebarButton("Backup wiederherstellen", RestoreBackup_Click);
                AddSidebarButton("Passwort ändern", ChangePassword_Click);
                AddSidebarButton("Logout", Logout_Click);
                break;
        }
    }

    private void AddSidebarButton(string text, RoutedEventHandler clickHandler)
    {
        var button = new Button
        {
            Content = text,
            Height = 40,
            Margin = new Thickness(0, 0, 0, 8),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(14, 0, 14, 0)
        };

        button.Click += clickHandler;
        DynamicSidebarPanel.Children.Add(button);
    }

    private void DummySidebarClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Diesen Bereich kannst du als Nächstes mit einem eigenen Tab oder UserControl ausbauen.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SwitchModule(BottomModule module)
    {
        _currentModule = module;

        BuildSidebar(module);

        switch (module)
        {
            case BottomModule.Patienten:
                SelectTab(0);
                break;

            case BottomModule.Labor:
                SelectTab(5);
                break;

            case BottomModule.Abrechnung:
                SelectTab(6);
                break;

            case BottomModule.Auswertungen:
                SelectTab(7);
                break;

            case BottomModule.Nachrichten:
                SelectTab(8);
                break;
        }
    }
    private void BottomPatients_Click(object sender, RoutedEventArgs e) => SwitchModule(BottomModule.Patienten);
    private void BottomLabor_Click(object sender, RoutedEventArgs e) => SwitchModule(BottomModule.Labor);
    private void BottomBilling_Click(object sender, RoutedEventArgs e) => SwitchModule(BottomModule.Abrechnung);
    private void BottomReports_Click(object sender, RoutedEventArgs e) => SwitchModule(BottomModule.Auswertungen);
    private void BottomMessages_Click(object sender, RoutedEventArgs e) => SwitchModule(BottomModule.Nachrichten);
    private void BottomCatalogs_Click(object sender, RoutedEventArgs e) => SwitchModule(BottomModule.Kataloge);
    private void BottomSetup_Click(object sender, RoutedEventArgs e) => SwitchModule(BottomModule.Einrichtung);
    private void BottomSettings_Click(object sender, RoutedEventArgs e) => SwitchModule(BottomModule.Einstellungen);

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

    private async void SelectTab(int index)
    {
        UpdateBottomNavigationState();

        if (MainTabControl != null)
            MainTabControl.SelectedIndex = index;

        switch (index)
        {
            case 1:
                await LoadAppointmentsTabAsync();
                break;
            case 2:
                await LoadDocumentsTabAsync();
                break;
            case 3:
                await LoadWaitingRoomTabAsync();
                break;
            case 4:
                await LoadUsersTabAsync();
                break;
        }
    }

    private void ShowPatientsTab_Click(object sender, RoutedEventArgs e)
    {
        SelectTab(0);
    }

    private void AppointmentsTabGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AppointmentsTabGrid.SelectedItem is AppointmentTabRow row)
        {
            SelectedAppointmentPatientText.Text = row.PatientName;
            SelectedAppointmentDateText.Text = row.DateText;
            SelectedAppointmentTimeText.Text = row.TimeText;
            SelectedAppointmentReasonText.Text = string.IsNullOrWhiteSpace(row.Reason) ? "-" : row.Reason;
            SelectedAppointmentStatusText.Text = row.Status;
        }
        else
        {
            SelectedAppointmentPatientText.Text = "-";
            SelectedAppointmentDateText.Text = "-";
            SelectedAppointmentTimeText.Text = "-";
            SelectedAppointmentReasonText.Text = "-";
            SelectedAppointmentStatusText.Text = "-";
        }
    }

    private void DocumentsTabGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DocumentsTabGrid.SelectedItem is DocumentTabRow row)
        {
            SelectedDocumentTitleText.Text = row.Title;
            SelectedDocumentPatientText.Text = row.PatientName;
            SelectedDocumentCategoryText.Text = row.Category;
            SelectedDocumentFileText.Text = row.FileName;
        }
        else
        {
            SelectedDocumentTitleText.Text = "-";
            SelectedDocumentPatientText.Text = "-";
            SelectedDocumentCategoryText.Text = "-";
            SelectedDocumentFileText.Text = "-";
        }
    }

    private void WaitingRoomTabGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WaitingRoomTabGrid.SelectedItem is WaitingRoomTabRow row)
        {
            SelectedWaitingPatientText.Text = row.PatientName;
            SelectedWaitingArrivalText.Text = row.ArrivalTimeText;
            SelectedWaitingDurationText.Text = row.WaitingDurationText;
            SelectedWaitingStatusText.Text = row.Status;
            SelectedWaitingRoomText.Text = row.Room;
        }
        else
        {
            SelectedWaitingPatientText.Text = "-";
            SelectedWaitingArrivalText.Text = "-";
            SelectedWaitingDurationText.Text = "-";
            SelectedWaitingStatusText.Text = "-";
            SelectedWaitingRoomText.Text = "-";
        }
    }

    private void UsersTabGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UsersTabGrid.SelectedItem is UserTabRow row)
        {
            SelectedUserNameText.Text = row.Username;
            SelectedUserRoleText.Text = row.Role;
            SelectedUserStatusText.Text = row.IsActive ? "Aktiv" : "Inaktiv";
            SelectedUserCreatedText.Text = row.CreatedAt.ToString("dd.MM.yyyy");
        }
        else
        {
            SelectedUserNameText.Text = "-";
            SelectedUserRoleText.Text = "-";
            SelectedUserStatusText.Text = "-";
            SelectedUserCreatedText.Text = "-";
        }
    }
    private async void ToggleUserActive_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (UsersTabGrid.SelectedItem is not UserTabRow row)
            {
                MessageBox.Show("Bitte zuerst einen Benutzer auswählen.");
                return;
            }

            if (row.Role == Roles.Administrator &&
                UserSession.CurrentUser?.Id == row.Id)
            {
                MessageBox.Show("Der aktuell angemeldete Administrator kann sich nicht selbst deaktivieren.");
                return;
            }

            await _userManagementService.ToggleUserActiveAsync(row.Id);
            await LoadUsersTabAsync();

            MessageBox.Show("Benutzerstatus wurde erfolgreich geändert.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim Ändern des Benutzerstatus:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    private async void CreateUserTab_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!UserSession.HasRole(Roles.Administrator))
            {
                MessageBox.Show("Nur Administratoren dürfen Benutzer anlegen.");
                return;
            }

            var window = App.ServiceProvider.GetRequiredService<UserManagementWindow>();
            window.Owner = this;

            if (window.ShowDialog() == true)
            {
                await LoadUsersTabAsync();
                MessageBox.Show("Benutzer wurde erfolgreich angelegt.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim Anlegen des Benutzers:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void ChangeUserRoleTab_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!UserSession.HasRole(Roles.Administrator))
            {
                MessageBox.Show("Nur Administratoren dürfen Rollen ändern.");
                return;
            }

            var window = App.ServiceProvider.GetRequiredService<UserManagementWindow>();
            window.Owner = this;

            window.ShowDialog();

            await LoadUsersTabAsync();
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
    private void UpdateBottomNavigationState()
    {

    }
}