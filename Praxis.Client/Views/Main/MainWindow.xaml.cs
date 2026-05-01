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
using System.Linq;
using System.Windows.Threading;
using Praxis.Client.Views.Pages.Dashboard;

using Praxis.Client.Views.Pages.UserManagement;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using Praxis.Client.Views.Pages.Abrechnung;
using Praxis.Client.Views.Pages.Kataloge;
using Praxis.Application.Interfaces;
using Praxis.Client.Views.Pages.Patienten.PatientAppointment;
using Praxis.Client.Security;
using Praxis.Client.ViewModels;

using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;
using MouseEventHandler = System.Windows.Input.MouseEventHandler;
using KeyEventHandler = System.Windows.Input.KeyEventHandler;


namespace Praxis.Client.Views.Main
{
    public partial class MainWindow : Window
    {
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
        private readonly RoomsPage _roomsPage;
        private readonly PatientAppointmentsPage _patientAppointmentsPage;
        private readonly DoctorsPage _doctorsPage;
        private readonly CatalogsPage _catalogsPage;
        private readonly ReportsPage _reportsPage;
        private readonly MessagesPage _messagesPage;

        private readonly DashboardPage _dashboardPage;
        private readonly PatientSearchPage _patientSearchPage = new PatientSearchPage();
        private readonly PatientCreatePage _patientCreatePage = new PatientCreatePage();
        private readonly PatientEditPage _patientEditPage;
        private readonly UserManagementPage _userManagementPage = new UserManagementPage();
        private readonly AddUserPage _addUserPage = new AddUserPage();
        private readonly EditUserPage _editUserPage = new EditUserPage();
        private readonly PatientDeletePage _patientDeletePage = new PatientDeletePage();
        private readonly PatientDocumentsPage _patientDocumentsPage = new PatientDocumentsPage();
    
        private readonly SettingsPage _settingsPage;

        private readonly IPatientService _patientService;
        private readonly IAppointmentService _appointmentService;
        private readonly IAuthService _authService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDashboardService _dashboardService;
        private readonly IAuditService _auditService;
        
        private readonly IDocumentService _documentService;
        private readonly IUserManagementService _userManagementService;        
        private readonly ILaborService _laborService;
        private readonly IAbrechnungService _abrechnungService;
        private readonly IDashboardTaskService _dashboardTaskService;
        private readonly IPracticeNoticeService _practiceNoticeService;
        private readonly IDashboardLayoutService _dashboardLayoutService;
        private readonly IRoomService _roomService;
        private readonly IAppointmentTypeService _appointmentTypeService;
        private readonly IDoctorService _doctorService;

        private readonly IPracticeMessageService _messageService;
        
        private Patient? _selectedPatient;

        private DispatcherTimer _sessionTimer;
        private DispatcherTimer _warningTimer;
        public IServiceProvider ServiceProvider => _serviceProvider;

        private readonly TimeSpan _timeout = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _warningTime = TimeSpan.FromMinutes(4);
        //private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
        //private readonly TimeSpan _warningTime = TimeSpan.FromSeconds(20);

        private DispatcherTimer? _messageRefreshTimer;
        private DateTime _lastActivityTime;               

        public MainWindow(
                             IPatientService patientService,
                             IAppointmentService appointmentService,
                             IAuthService authService,
                             IServiceProvider serviceProvider,
                             IDashboardService dashboardService,
                             IAuditService auditService,                             
                             IUserManagementService userManagementService,
                             IDocumentService documentService,
                             ILaborService laborService,
                             IAbrechnungService abrechnungService,
                             IDashboardTaskService dashboardTaskService,
                             IPracticeNoticeService practiceNoticeService,
                             IDashboardLayoutService dashboardLayoutService,
                             IRoomService roomService,
                             IDoctorService doctorService,
                             IAppointmentTypeService appointmentTypeService,
                             ICatalogService catalogService,
                             IIcdImportService icdImportService,
                             IMedicationImportService medicationImportService,
                             IServiceCatalogImportService serviceCatalogImportService,
                             IBackupService backupService,
                             IThemeService themeService,
                             IPatientDiagnosisService patientDiagnosisService,
                             IPatientMedicationService patientMedicationService,
                             IPracticeSettingsService practiceSettingsService,
                             IAppointmentMedicalEntryService appointmentMedicalEntryService,
                             IInvoiceService invoiceService,
                             IInvoicePdfService invoicePdfService,
                             IBillingGenerationService billingGenerationService,
                             IPatientMedicalRecordService medicalRecordService,
                             IReportsService reportsService,
                             IPracticeMessageService messageService,
                             IDoctorLetterService doctorLetterService,
                             IExternalMessageService externalMessageService
                             )
        {
            InitializeComponent();

            _messageRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };

            _messageRefreshTimer.Tick += async (s, e) =>
            {
                if (_currentModule == BottomModule.Nachrichten)
                {
                    await _messagesPage.RefreshAsync();
                    RefreshSidebarForCurrentModule();
                }
            };

            _messageRefreshTimer.Start();


            _patientService = patientService;
            _appointmentService = appointmentService;
            _authService = authService;
            _serviceProvider = serviceProvider;
            _dashboardService = dashboardService;
            _auditService = auditService;
            _documentService = documentService;
            _userManagementService = userManagementService;
            Loaded += Window_Loaded;
            _laborService = laborService;
            _abrechnungService = abrechnungService;
            _dashboardTaskService = dashboardTaskService;
            _practiceNoticeService = practiceNoticeService;
            _messageService = messageService;

            _dashboardLayoutService = dashboardLayoutService;

            _roomService = roomService;

            _laborPage = new LaborPage(_laborService);
            _abrechnungPage = new AbrechnungPage(   _abrechnungService,
                                                    invoiceService,
                                                    invoicePdfService,
                                                    billingGenerationService,
                                                    medicalRecordService);
            _waitingRoomPage = new WaitingRoomPage(_appointmentService);
            _roomsPage = new RoomsPage(_roomService);
            _doctorsPage = new DoctorsPage(doctorService, _roomService, appointmentTypeService);
            _patientAppointmentsPage = new PatientAppointmentsPage(
                                                                    appointmentService,
                                                                    roomService,
                                                                    patientService,
                                                                    appointmentMedicalEntryService);
            _catalogsPage = new CatalogsPage(catalogService,
                                            icdImportService,
                                            medicationImportService,
                                            serviceCatalogImportService);

            _settingsPage = new SettingsPage(
                                             authService,
                                             themeService,
                                             backupService,
                                             practiceSettingsService);
            _patientEditPage = new PatientEditPage(
                                                    patientDiagnosisService,
                                                    patientMedicationService,
                                                    practiceSettingsService);
            _reportsPage = new ReportsPage(reportsService);
            _messagesPage = new MessagesPage(messageService,
                                             patientService,
                                             practiceNoticeService,
                                             doctorLetterService,
                                             externalMessageService
                                             );

            var dashboardViewModel = new DashboardViewModel();

            _dashboardPage = new DashboardPage(dashboardViewModel);
            StartSessionTimer();
           
        }          
       
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLoggedInUserDisplay();
            try
            {
                SwitchModule(BottomModule.Patienten);
                SetInitialBottomButton();
                await RefreshBottomStatusAsync();

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
    }
}



