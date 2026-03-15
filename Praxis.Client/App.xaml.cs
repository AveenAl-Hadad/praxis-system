using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Praxis.Client.Views;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;
using Praxis.Infrastructure.Services.Interface;

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
                services.AddTransient<IDashboardService, DashboardService>();
                services.AddTransient<IEmailService, EmailService>();
                services.AddTransient<IReminderService, ReminderService>();


                services.AddTransient<MainWindow>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<WaitingRoomWindow>();
                services.AddTransient<AddAppointmentWindow>();
                services.AddTransient<AppointmentWindow>();
                services.AddTransient<AppointmentCalendarWindow>();
                services.AddTransient<AddPatientWindow>();
                services.AddTransient<UserManagementWindow>();
                services.AddTransient<InvoiceWindow>();
                services.AddTransient<AddInvoiceWindow>();
                services.AddTransient<ChangePasswordWindow>();
                services.AddTransient<PrescriptionWindow>();
                services.AddTransient<AddPrescriptionWindow>();
                services.AddTransient<DocumentWindow>();
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
                    Telefonnummer = "0000000000",
                    IsActive = true
                });

                await db.SaveChangesAsync();
            }

            if (!db.Users.Any())
            {
                await authService.RegisterUserAsync("admin", "admin123", Roles.Administrator);
            }
        }

        try
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
            var loginResult = loginWindow.ShowDialog();

            if (loginResult == true)
            {
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                MainWindow = mainWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.Show();
            }
            else
            {
                Shutdown();
            }
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
}