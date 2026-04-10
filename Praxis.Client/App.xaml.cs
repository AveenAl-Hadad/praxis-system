using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Praxis.Application.Services;
using Praxis.Client.Logic.UI;
using Praxis.Client.Views;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;
using Praxis.Infrastructure.Services.Interface;
using MessageBox = System.Windows.MessageBox;
namespace Praxis.Client;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        var culture = new CultureInfo("de-DE");

        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(AppContext.BaseDirectory);
                cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                var dbFile = ctx.Configuration["Database:FileName"] ?? "praxis.db";
                var dbPath = Path.Combine(AppContext.BaseDirectory, dbFile);

                services.AddInfrastructure(dbPath);

                services.AddTransient<IPatientService, PatientService>();
                services.AddTransient<IAppointmentService, AppointmentService>();
                services.AddTransient<IPasswordService, PasswordService>();
                services.AddTransient<IAuthService, AuthService>();
                services.AddTransient<IUserManagementService, UserManagementService>();
                services.AddTransient<IInvoiceService, InvoiceService>();
                services.AddTransient<IInvoicePdfService, InvoicePdfService>();
                services.AddTransient<IPrescriptionService, PrescriptionService>();
                services.AddTransient<IPrescriptionPdfService, PrescriptionPdfService>();
                services.AddTransient<IDocumentService, DocumentService>();
               ;
                services.AddTransient<IEmailService, EmailService>();
                services.AddTransient<IReminderService, ReminderService>();
                services.AddTransient<IAuditService, AuditService>();
                services.AddTransient<IBackupService, BackupService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddTransient<ILaborService, LaborService>();
                services.AddTransient<IAbrechnungService, AbrechnungService>();

                services.AddTransient<IDashboardService, DashboardService>();
                services.AddTransient<IDashboardTaskService, DashboardTaskService>();
                services.AddTransient<IPracticeNoticeService, PracticeNoticeService>();

                services.AddTransient<MainWindow>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<ChangePasswordWindow>();
                services.AddTransient<AuditLogWindow>();
                services.AddTransient<OnlineBookingWindow>();
                services.AddTransient<TaskEditWindow>();


            })
            .Build();

        ServiceProvider = _host.Services;

        using (var initScope = _host.Services.CreateScope())
        {
            var db = initScope.ServiceProvider.GetRequiredService<PraxisDbContext>();
            var authService = initScope.ServiceProvider.GetRequiredService<IAuthService>();

            await db.Database.EnsureCreatedAsync();

            if (!db.Patients.Any())
            {
                db.Patients.Add(new Patient
                {
                    Vorname = "Max",
                    Nachname = "Mustermann",
                    Geburtsdatum = new DateTime(1980, 1, 1),
                    Email = "max@test.de",
                    Telefonnummer = "12456987",
                    IsActive = true
                });

                await db.SaveChangesAsync();
            }

            if (!db.Users.Any())
            {
                await authService.RegisterUserAsync("admin", "admin123", Roles.Administrator);
            }
        }
        await SeedDashboardDataAsync(_host.Services);

        try
        {
            ShutdownMode = ShutdownMode.OnLastWindowClose;

            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
            MainWindow = loginWindow;
            loginWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim Starten der Anwendung:\n{ex.Message}",
                "Startfehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private async Task SeedDashboardDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var taskService = scope.ServiceProvider.GetRequiredService<IDashboardTaskService>();
        var noticeService = scope.ServiceProvider.GetRequiredService<IPracticeNoticeService>();

        // Prüfen ob schon Daten existieren
        var existingTasks = await taskService.GetOpenTasksAsync();
        var existingNotices = await noticeService.GetActiveNoticesAsync();

        if (!existingTasks.Any())
        {
            await taskService.AddTaskAsync(new DashboardTask
            {
                Title = "Laborwerte kontrollieren",
                Description = "Laborergebnisse vom Morgen prüfen",
                Priority = "Hoch",
                DueDate = DateTime.Today,
                AssignedTo = "Dr. Mustermann"
            });

            await taskService.AddTaskAsync(new DashboardTask
            {
                Title = "Rückruf Patient",
                Description = "Patient wegen Befund informieren",
                Priority = "Normal",
                DueDate = DateTime.Today.AddDays(1),
                AssignedTo = "Anmeldung"
            });
        }

        if (!existingNotices.Any())
        {
            await noticeService.AddNoticeAsync(new PracticeNotice
            {
                Title = "TI-Wartung",
                Content = "Heute zwischen 12:00 und 12:30 kann es zu Unterbrechungen kommen.",
                Category = "Wichtig",
                IsPinned = true
            });

            await noticeService.AddNoticeAsync(new PracticeNotice
            {
                Title = "Vertretung",
                Content = "Dr. Meier übernimmt heute Zimmer 2.",
                Category = "Info"
            });
        }
    }
}