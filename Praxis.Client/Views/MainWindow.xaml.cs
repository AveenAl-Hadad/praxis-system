using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Client.Logic.UI;
using Praxis.Client.Session;
using Praxis.Client.Views.Pages;
using Praxis.Client.Views.Pages.Patienten;
using Praxis.Client.Views.Pages.Labor;

using Praxis.Client.Views.Pages.UserManagement;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using Praxis.Client.Views.Pages.Abrechnung;

using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;
using System.Windows.Threading;
using Praxis.Application.Interfaces;
using MouseEventHandler = System.Windows.Input.MouseEventHandler;
using KeyEventHandler = System.Windows.Input.KeyEventHandler;


namespace Praxis.Client.Views
{
    public partial class MainWindow : Window
    {
        #region Variablen Defenation
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

        private Button _activeSidebarButton;
        private Button _activeBottomButton;

        private readonly LaborPage _laborPage;
        private readonly AbrechnungPage _abrechnungPage;
        private readonly WaitingRoomPage _waitingRoomPage;

        private readonly ReportsPage _reportsPage = new ReportsPage();
        private readonly MessagesPage _messagesPage = new MessagesPage();
        private readonly DashboardPage _dashboardPage = new DashboardPage();
        private readonly PatientSearchPage _patientSearchPage = new PatientSearchPage();
        private readonly PatientCreatePage _patientCreatePage = new PatientCreatePage();
        private readonly PatientEditPage _patientEditPage = new PatientEditPage();
        private readonly UserManagementPage _userManagementPage = new UserManagementPage();
        private readonly AddUserPage _addUserPage = new AddUserPage();
        private readonly EditUserPage _editUserPage = new EditUserPage();
        private readonly PatientDeletePage _patientDeletePage = new PatientDeletePage();
        private readonly PatientDocumentsPage _patientDocumentsPage = new PatientDocumentsPage();
        private readonly PatientAppointmentsPage _patientAppointmentsPage = new PatientAppointmentsPage();
      

        private readonly IPatientService _patientService;
        private readonly IAppointmentService _appointmentService;
        private readonly IAuthService _authService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDashboardService _dashboardService;
        private readonly IBackupService _backupService;
        private readonly IAuditService _auditService;
        private readonly IThemeService _themeService;
        private readonly IDocumentService _documentService;
        private readonly IUserManagementService _userManagementService;        
        private readonly ILaborService _laborService;
        private readonly IAbrechnungService _abrechnungService;
        private readonly IDashboardTaskService _dashboardTaskService;
        private readonly IPracticeNoticeService _practiceNoticeService;
        private readonly IDashboardLayoutService _dashboardLayoutService;



        private Patient? _selectedPatient;

        private DispatcherTimer _sessionTimer;
        private DispatcherTimer _warningTimer;

        private readonly TimeSpan _timeout = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _warningTime = TimeSpan.FromMinutes(4);
        //private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
        //private readonly TimeSpan _warningTime = TimeSpan.FromSeconds(20);

        #endregion
        #region Konstrakture
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
                             IDocumentService documentService,
                             ILaborService laborService,
                             IAbrechnungService abrechnungService,
                             IDashboardTaskService dashboardTaskService,
                             IPracticeNoticeService practiceNoticeService,
                             IDashboardLayoutService dashboardLayoutService)
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
            Loaded += Window_Loaded;
            _laborService = laborService;
            _abrechnungService = abrechnungService;
            _dashboardTaskService = dashboardTaskService;
            _practiceNoticeService = practiceNoticeService;


            _laborPage = new LaborPage(_laborService);
            _abrechnungPage = new AbrechnungPage(_abrechnungService);
            _waitingRoomPage = new WaitingRoomPage(_appointmentService);

            StartSessionTimer();
            _dashboardLayoutService = dashboardLayoutService;
        }
        #endregion
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLoggedInUserDisplay();
            try
            {
                SwitchModule(BottomModule.Patienten);
                SetInitialBottomButton();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Laden des Hauptfensters:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // optional
            }
        }

        #region Seiten laden
        private void LoadPage(System.Windows.Controls.UserControl page)
        {
            if (MainContentControl != null)
                MainContentControl.Content = page;
        }
        private void SwitchModule(BottomModule module)
        {
            _currentModule = module;
            BuildSidebar(module);

            switch (module)
            {
                case BottomModule.Patienten:
                    LoadPage(_dashboardPage);
                    _ = _dashboardPage.RefreshAsync();
                    break;

                case BottomModule.Labor:
                    LoadPage(_laborPage);
                    break;

                case BottomModule.Abrechnung:
                    LoadPage(_abrechnungPage);
                    break;

                case BottomModule.Auswertungen:
                    LoadPage(_reportsPage);
                    break;

                case BottomModule.Nachrichten:
                    LoadPage(_messagesPage);
                    break;

                case BottomModule.Kataloge:
                    LoadPlaceholderPage("Kataloge");
                    break;

                case BottomModule.Einrichtung:
                    if(IsAdmin())
                        LoadPage(_userManagementPage);
                    else
                        LoadPlaceholderPage("Keine Berechteigung");
                    break;

                case BottomModule.Einstellungen:
                    LoadPlaceholderPage("Einstellungen");
                    break;

                default:
                    LoadPage(_patientSearchPage);
                    break;
            }
        }
        private void LoadPlaceholderPage(string title)
        {
            var grid = new Grid
            {
                Margin = new Thickness(16)
            };

            var text = new TextBlock
            {
                Text = $"{title} – Bereich folgt als Nächstes",
                FontSize = 24,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };

            grid.Children.Add(text);
            MainContentControl.Content = grid;
        }
        private void UpdateLoggedInUserDisplay()
        {
            try
            {
                var userName = UserSession.CurrentUser?.Username;
                var role = UserSession.CurrentUser?.Role;

                var displayName = !string.IsNullOrWhiteSpace(userName)
                    ? userName
                    : "Unbekannter Benutzer";

                LoggedInUserText.Text = displayName;
                LoggedInStatusText.Text = !string.IsNullOrWhiteSpace(role)
                    ? $"Angemeldet als {role}"
                    : "Angemeldet";

                UserInitialText.Text = GetInitials(displayName);
            }
            catch
            {
                LoggedInUserText.Text = "Benutzer";
                LoggedInStatusText.Text = "Angemeldet";
                UserInitialText.Text = "B";
            }
        }
        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
                return parts[0].Substring(0, 1).ToUpper();

            return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
        }
        
        #endregion

        #region Sidebar
        private bool IsAdmin()
        {
            return UserSession.HasRole(Roles.Administrator) || UserSession.HasRole("Admin");
        }
        private void BuildSidebar(BottomModule module)
        {
            if (DynamicSidebarPanel == null)
                return;

            DynamicSidebarPanel.Children.Clear();
            _activeSidebarButton = null;

            switch (module)
            {
                case BottomModule.Patienten:
                    AddSidebarButton("Dashboard", async (s, e) => {LoadPage(_dashboardPage); await _dashboardPage.RefreshAsync();}, true);
                    AddSidebarButton("Suche", async(s, e) => await OpenPatientSearchPageAsync(), true);
                    AddSidebarButton("Neuer Patient", (s, e) => OpenPatientCreatePage());
                    AddSidebarButton("Bearbeiten", async (s, e) => await OpenPatientEditPageAsync());
                    AddSidebarButton("Löschen", async (s, e) => await OpenPatientDeletePageAsync());
                    AddSidebarButton("Dokumente", async (s, e) => await OpenSelectedPatientDocumentsPageAsync());
                    AddSidebarButton("Termine", async (s, e) => await OpenSelectedPatientAppointmentsPageAsync());
                    AddSidebarButton("Wartezimmer", async (s, e) => {LoadPage(_waitingRoomPage); await _waitingRoomPage.RefreshAsync();});
                    break;

                case BottomModule.Labor:
                    AddSidebarButton("Labordaten importieren", (s, e) => LoadPage(_laborPage), true);
                    AddSidebarButton("Laborbücher zuordnen", (s, e) => MessageBox.Show("Bereich 'Laborbücher zuordnen' folgt als Nächstes."));
                    AddSidebarButton("Zugeordnete Laborberichte", (s, e) => MessageBox.Show("Bereich 'Zugeordnete Laborberichte' folgt als Nächstes."));
                    AddSidebarButton("Labortagesliste", (s, e) => MessageBox.Show("Bereich 'Labortagesliste' folgt als Nächstes."));
                    AddSidebarButton("Labore", (s, e) => MessageBox.Show("Bereich 'Labore' folgt als Nächstes."));
                    break;

                case BottomModule.Abrechnung:
                    AddSidebarButton("Neue KV-Abrechnung", (s, e) => LoadPage(_abrechnungPage), true);
                    AddSidebarButton("KV-Abrechnungen", DummySidebarClick);
                    AddSidebarButton("Neue Privatabrechnung", DummySidebarClick);
                    AddSidebarButton("Rechnungen", OpenInvoices_Click);
                    AddSidebarButton("Mahnungen", DummySidebarClick);
                    break;

                case BottomModule.Auswertungen:
                    AddSidebarButton("Checkliste", (s, e) => LoadPage(_reportsPage), true);
                    AddSidebarButton("Patienten ohne Karte", DummySidebarClick);
                    AddSidebarButton("Leistungsziffern-Statistik", DummySidebarClick);
                    AddSidebarButton("Diagnose-Statistik", DummySidebarClick);
                    AddSidebarButton("Patienten-Statistik", DummySidebarClick);
                    break;

                case BottomModule.Nachrichten:
                    AddSidebarButton("Neue Nachricht", (s, e) => LoadPage(_messagesPage), true);
                    AddSidebarButton("Interne Nachrichten", DummySidebarClick);
                    AddSidebarButton("Externe Nachrichten", DummySidebarClick);
                    AddSidebarButton("Notizen", DummySidebarClick);
                    AddSidebarButton("Arztbriefe", DummySidebarClick);
                    break;

                case BottomModule.Kataloge:
                    AddSidebarButton("Katalogübersicht", DummySidebarClick, true);
                    AddSidebarButton("Einträge", DummySidebarClick);
                    break;

                case BottomModule.Einrichtung:
                    if (IsAdmin())
                    {
                        AddSidebarButton("Benutzer", (s, e) => LoadPage(_userManagementPage), true);
                        AddSidebarButton("Arbeitsplätze", DummySidebarClick);
                        AddSidebarButton("TI-Konfiguration", DummySidebarClick);
                        AddSidebarButton("Räume", DummySidebarClick);
                        AddSidebarButton("Rollen", DummySidebarClick);
                    }
                    else
                    {
                        AddSidebarButton("Keine Berechtigung", DummySidebarClick, true);
                    }
                    break;

                case BottomModule.Einstellungen:
                    AddSidebarButton("Design", DummySidebarClick, true);
                    AddSidebarButton("Passwort ändern", ChangePassword_Click);
                    AddSidebarButton("Backup", CreateBackup_Click);
                    AddSidebarButton("Restore", RestoreBackup_Click);
                    break;
            }
        }

        private void AddSidebarButton(string text, RoutedEventHandler clickHandler, bool setActive = false)
        {
            var btn = new Button
            {
                Content = text,
                Margin = new Thickness(0, 0, 0, 8),
                Style = TryFindResource("SidebarButtonStyle") as Style
            };

            btn.Click += (s, e) =>
            {
                SetActiveSidebarButton(btn);
                clickHandler?.Invoke(s, e);
            };

            DynamicSidebarPanel.Children.Add(btn);

            if (setActive)
                SetActiveSidebarButton(btn);
        }

        private void SetActiveSidebarButton(Button btn)
        {
            if (_activeSidebarButton != null)
            {
                var normalStyle = TryFindResource("SidebarButtonStyle") as Style;
                if (normalStyle != null)
                    _activeSidebarButton.Style = normalStyle;
            }

            _activeSidebarButton = btn;

            var activeStyle = TryFindResource("SidebarButtonActiveStyle") as Style;
            if (activeStyle != null)
                _activeSidebarButton.Style = activeStyle;
        }

        private void DummySidebarClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Diese Funktion baust du als Nächstes ein.",
                "Hinweis",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }


        #endregion

        #region Bottom Navigation

        private void SetActiveBottomButton(Button btn)
        {
            if (_activeBottomButton != null)
                _activeBottomButton.Tag = null;

            _activeBottomButton = btn;
            _activeBottomButton.Tag = "Active";
        }

        private void SetInitialBottomButton()
        {
            if (BottomPatientsButton != null)
                SetActiveBottomButton(BottomPatientsButton);
        }

        private void BottomPatients_Click(object sender, RoutedEventArgs e)
        {
            SetActiveBottomButton((Button)sender);
            SwitchModule(BottomModule.Patienten);
        }

        private void BottomLabor_Click(object sender, RoutedEventArgs e)
        {
            SetActiveBottomButton((Button)sender);
            SwitchModule(BottomModule.Labor);
        }

        private void BottomBilling_Click(object sender, RoutedEventArgs e)
        {
            SetActiveBottomButton((Button)sender);
            SwitchModule(BottomModule.Abrechnung);
        }

        private void BottomReports_Click(object sender, RoutedEventArgs e)
        {
            SetActiveBottomButton((Button)sender);
            SwitchModule(BottomModule.Auswertungen);
        }

        private void BottomMessages_Click(object sender, RoutedEventArgs e)
        {
            SetActiveBottomButton((Button)sender);
            SwitchModule(BottomModule.Nachrichten);
        }

        private void BottomCatalogs_Click(object sender, RoutedEventArgs e)
        {
            SetActiveBottomButton((Button)sender);
            SwitchModule(BottomModule.Kataloge);
        }

        private void BottomSetup_Click(object sender, RoutedEventArgs e)
        {
            SetActiveBottomButton((Button)sender);
            SwitchModule(BottomModule.Einrichtung);
        }

        private void BottomSettings_Click(object sender, RoutedEventArgs e)
        {
            SetActiveBottomButton((Button)sender);
            SwitchModule(BottomModule.Einstellungen);
        }

        #endregion

        #region Patienten Bereich
        public async Task<IEnumerable<Patient>> GetPatientsAsync()
        {
            return await _patientService.GetAllPatientsAsync();
        }
        public async Task ReloadPatientSearchPageAsync()
        {
            await _patientSearchPage.RefreshAsync();
        }
        public async Task LoadDashboardAsync()
        {
            try
            {
                if (_currentModule == BottomModule.Patienten)
                {
                    LoadPage(_dashboardPage);
                    await _dashboardPage.RefreshAsync();
                    return;
                }

                switch (_currentModule)
                {
                    case BottomModule.Labor:
                        LoadPage(_laborPage);
                        break;

                    case BottomModule.Abrechnung:
                        LoadPage(_abrechnungPage);
                        break;

                    case BottomModule.Auswertungen:
                        LoadPage(_reportsPage);
                        break;

                    case BottomModule.Nachrichten:
                        LoadPage(_messagesPage);
                        break;

                    case BottomModule.Kataloge:
                        LoadPlaceholderPage("Kataloge");
                        break;

                    case BottomModule.Einrichtung:
                        LoadPlaceholderPage("Einrichtung");
                        break;

                    case BottomModule.Einstellungen:
                        LoadPlaceholderPage("Einstellungen");
                        break;

                    default:
                        LoadPage(_dashboardPage);
                        await _dashboardPage.RefreshAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Aktualisieren der Hauptansicht:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        public async Task CreatePatientAsync(Patient patient)
        {
            var userName = UserSession.CurrentUser?.Username ?? "system";
            await _patientService.AddPatientAsync(patient, userName);
        }
        public async Task OpenPatientSearchPageAsync()
        {
            LoadPage(_patientSearchPage);
            await _patientSearchPage.RefreshAsync();
        }
        public void OpenPatientCreatePage()
        {
            LoadPage(_patientCreatePage);
        }
        public async Task UpdatePatientAysnc(Patient patient)
        {
            await _patientService.UpdatePatientAsync(patient);
        }
        public async Task OpenPatientEditPageAsync()
        {
            LoadPage(_patientEditPage);
            await _patientEditPage.RefreshAsync();
        }
        public async void OpenEditPatientPage(Patient patient)
        {
            LoadPage(_patientEditPage);
            await _patientEditPage.LoadPatientAsync(patient);
        }
        public async Task OpenPatientDeletePageAsync()
        {
            LoadPage(_patientDeletePage);
            await _patientDeletePage.RefreshAsync();
        }
        public async Task DeletePatientByIdAsync(int patientId)
        {
            var userName = UserSession.CurrentUser?.Username ?? "system";
            await _patientService.DeletePatientAsync(patientId, userName);
        }
        public async Task OpenPatientDocumentsPageAsync(Patient patient)
        {
            LoadPage(_patientDocumentsPage);
            await _patientDocumentsPage.LoadPatientAsync(patient);
        }

        public async Task OpenPatientAppointmentsPageAsync(Patient patient)
        {
            LoadPage(_patientAppointmentsPage);
            await _patientAppointmentsPage.LoadPatientAsync(patient);
        }
        public async Task OpenSelectedPatientDocumentsPageAsync()
        {
            if (_selectedPatient == null)
            {
                MessageBox.Show("Bitte zuerst in der Patientensuche einen Patienten auswählen oder doppelt anklicken.");
                return;
            }

            await OpenPatientDocumentsPageAsync(_selectedPatient);
        }

        public async Task OpenSelectedPatientAppointmentsPageAsync()
        {
            if (_selectedPatient == null)
            {
                MessageBox.Show("Bitte zuerst in der Patientensuche einen Patienten auswählen oder doppelt anklicken.");
                return;
            }

            await OpenPatientAppointmentsPageAsync(_selectedPatient);
        }
        public async Task<IEnumerable<PatientDocument>> GetDocumentsByPatientIdAsync(int patientId)
        {
            return await _documentService.GetDocumentsByPatientAsync(patientId);
        }
        public async Task<IEnumerable<Appointment>> GetAppointmentsByPatientIdAsync(int patientId)
        {
            var allAppointments = await _appointmentService.GetAllAppointmentsAsync();
            return allAppointments
                .Where(a => a.PatientId == patientId)
                .OrderBy(a => a.StartTime)
                .ToList();
        }
        public void SetSelectedPatient(Patient patient)
        {
            _selectedPatient = patient;
        }
        public Patient? GetSelectedPatient()
        {
            return _selectedPatient;
        }
        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            return await _dashboardService.GetStatsAsync();
        }
        public async Task<IEnumerable<Appointment>> GetAppointmentsByDateAsync(DateTime date)
        {
            return await _appointmentService.GetAppointmentsByDateAsync(date);
        }
        public async Task<IEnumerable<DashboardTask>> GetOpenDashboardTasksAsync()
        {
            return await _dashboardTaskService.GetOpenTasksAsync();
        }
        public async Task<IEnumerable<PracticeNotice>> GetActivePracticeNoticesAsync()
        {
            return await _practiceNoticeService.GetActiveNoticesAsync();
        }
        public async Task AddDashboardTaskAsync(DashboardTask task)
        {
            await _dashboardTaskService.AddTaskAsync(task);
        }
        public async Task AddPracticeNoticeAsync(PracticeNotice notice)
        {
            await _practiceNoticeService.AddNoticeAsync(notice);
        }
        public async Task DeactivatePracticeNoticeAsync(int noticeId)
        {
            await _practiceNoticeService.DeactivateNoticeAsync(noticeId);
        }
        public async Task MarkDashboardTaskAsDoneAsync(int taskId)
        {
            await _dashboardTaskService.MarkAsDoneAsync(taskId);
        }
        public async Task<IEnumerable<DashboardTask>> GetAllDashboardTasksAsync()
        {
            return await _dashboardTaskService.GetAllTasksAsync();
        }
        public async Task<DashboardTask?> GetDashboardTaskByIdAsync(int taskId)
        {
            return await _dashboardTaskService.GetByIdAsync(taskId);
        }
        public async Task UpdateDashboardTaskAsync(DashboardTask task)
        {
            await _dashboardTaskService.UpdateTaskAsync(task);
        }
        public async Task UpdatePracticeNoticeAsync(PracticeNotice notice)
        {
            await _practiceNoticeService.UpdateNoticeAsync(notice);
        }
        public async Task DeleteDashboardTaskAsync(int taskId)
        {
            await _dashboardTaskService.DeleteTaskAsync(taskId);
        }

        public async Task MoveDashboardTaskToOpenAsync(int taskId)
        {
            var task = await _dashboardTaskService.GetByIdAsync(taskId);
            if (task == null)
                throw new InvalidOperationException("Aufgabe wurde nicht gefunden.");

            task.Status = "Offen";

            if (task.DueDate != null && task.DueDate.Value.Date <= DateTime.Today)
            {
                task.DueDate = DateTime.Today.AddDays(1);
            }

            await _dashboardTaskService.UpdateTaskAsync(task);
        }
        public async Task DeletePracticeNoticeAsync(int noticeId)
        {
            await _practiceNoticeService.DeleteNoticeAsync(noticeId);
        }
        public async Task<List<string>> GetDashboardWidgetOrderAsync()
        {
            var username = GetCurrentDashboardUsername();
            return await _dashboardLayoutService.GetWidgetOrderAsync(username);
        }

        public async Task SaveDashboardWidgetOrderAsync(List<string> widgetOrder)
        {
            var username = GetCurrentDashboardUsername();
            await _dashboardLayoutService.SaveWidgetOrderAsync(username, widgetOrder);
        }
        private string GetCurrentDashboardUsername()
        {
            return UserSession.CurrentUser?.Username ?? "default";
        }
        #endregion

        #region Open Bereich
        private void OpenAppointments_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hier öffnest du AppointmentWindow.");
        }

        private void OpenDocuments_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hier öffnest du DocumentWindow.");
        }

        private void OpenWaitingRoom_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hier öffnest du das Wartezimmer.");
        }

        private void OpenInvoices_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hier öffnest du InvoiceWindow.");
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hier öffnest du ChangePasswordWindow.");
        }

        private void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hier startest du Backup.");
        }

        private void RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hier startest du Restore.");
        }
        #endregion

        #region Benutzerverwaltung Bereich
        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            return await _userManagementService.GetAllUsersAsync();
        }
        public async Task<User> CreateUserAsync(string username, string password, string role)
        {
            return await _userManagementService.CreateUserAsync(username, password, role);
        }
        public async Task UpdateUserRoleAsync(int userId, string role)
        {
            await _userManagementService.UpdateUserRoleAsync(userId, role);
        }
        public async Task ResetUserPasswordAsync(int userId, string newPassword)
        {
            await _userManagementService.ResetPasswordAsync(userId, newPassword);
        }
        public async Task ToggleUserActiveAsync(int userId)
        {
            await _userManagementService.ToggleUserActiveAsync(userId);
        }
        public async Task DeleteUserAsync(int userId)
        {
            await _userManagementService.DeleteUserAsync(userId);
        }
        public async Task OpenUserManagementPageAsync()
        {
            LoadPage(_userManagementPage);
            await _userManagementPage.RefreshAsync();
        }
        public void OpenAddUserPage()
        {
            LoadPage(_addUserPage);
        }
        public void OpenEditUserPage(User user)
        {
            _editUserPage.SetUser(user);
            LoadPage(_editUserPage);
        }
        #endregion

        #region Automatisch Abmeldung
       
        private DateTime _lastActivityTime;

        private void StartSessionTimer()
        {
            _lastActivityTime = DateTime.Now;

            _sessionTimer = new DispatcherTimer();
            _sessionTimer.Interval = TimeSpan.FromSeconds(5);
            _sessionTimer.Tick += SessionTimer_Tick;
            _sessionTimer.Start();

            // Aktivität zuverlässig überwachen, auch wenn Controls Events selbst behandeln
            AddHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(ActivityDetected), true);
            AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(ActivityDetected), true);
            AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(ActivityDetected), true);
            AddHandler(UIElement.PreviewTextInputEvent, new TextCompositionEventHandler(ActivityDetected), true);
        }

        private void ActivityDetected(object sender, EventArgs e)
        {
            _lastActivityTime = DateTime.Now;
        }

        private void SessionTimer_Tick(object? sender, EventArgs e)
        {
            if (!UserSession.IsLoggedIn)
                return;

            var inactiveTime = DateTime.Now - _lastActivityTime;

            // Warnung 1 Minute vor Logout
            if (inactiveTime >= (_timeout - _warningTime) && inactiveTime < _timeout)
            {
                _sessionTimer.Stop();

                var result = MessageBox.Show(
                    $"Ihre Sitzung läuft in {_warningTime.Minutes} Minute(n) ab.\nMöchten Sie weiterarbeiten?",
                    "Session läuft ab",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _lastActivityTime = DateTime.Now;
                    _sessionTimer.Start();
                    return;
                }

                LogoutAuto();
                return;
            }

            // Logout nach kompletter Inaktivität
            if (inactiveTime >= _timeout)
            {
                _sessionTimer.Stop();
                LogoutAuto(true);
            }
        }

        private void LogoutAuto(bool showMessage = false)
        {
            _sessionTimer?.Stop();
            _warningTimer?.Stop();

            UserSession.Logout();

            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            System.Windows.Application.Current.MainWindow = loginWindow;
            loginWindow.Show();

            if (showMessage)
            {
                MessageBox.Show(
                    loginWindow,
                    "Sie wurden aufgrund von Inaktivität automatisch abgemeldet.",
                    "Session Timeout",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            this.Close();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Möchten Sie sich wirklich abmelden?",
                "Abmelden",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            _sessionTimer?.Stop();
            _warningTimer?.Stop();

            UserSession.Logout();

            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            System.Windows.Application.Current.MainWindow = loginWindow;
            loginWindow.Show();

            this.Close();
        }

        #endregion




    }
}



