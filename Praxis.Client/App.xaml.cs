using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
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
        base.OnStartup(e);
        
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(Directory.GetCurrentDirectory());
                cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                var dbFile = ctx.Configuration["Database:FileName"] ?? "praxis.db";
                var dbPath = Path.Combine(Directory.GetCurrentDirectory(), dbFile);

                // optional: anzeigen, wo die DB liegt
                // MessageBox.Show($"DB Pfad:\n{dbPath}");

                services.AddInfrastructure(dbPath);

                // Services
                services.AddScoped<IPatientService, PatientService>();
                services.AddScoped<IAppointmentService, AppointmentService>();
                services.AddScoped<IPasswordService, PasswordService>();
                services.AddScoped<IAuthService, AuthService>();

                // MainWindow über DI
                services.AddTransient<MainWindow>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<WaitingRoomWindow>();
                services.AddTransient<AddAppointmentWindow>();
                services.AddTransient<AppointmentWindow>();
                services.AddTransient<AppointmentCalendarWindow>();
                services.AddTransient<AddAppointmentWindow>();
                services.AddTransient<AddPatientWindow>();
                services.AddTransient<PatientDetailWindow>();

                ServiceProvider = services.BuildServiceProvider();
             })
            .Build();

        // Datenbank vorbereiten
        using (var scope = _host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PraxisDbContext>();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

            await db.Database.EnsureCreatedAsync();

            // TESTDATEN (nur wenn Tabelle leer ist)
            if (!db.Patients.Any())
            {
                db.Patients.Add(new Patient
                {
                    Vorname = "Max",
                    Nachname = "Mustermann",
                    Geburtsdatum = new DateTime(1980, 1, 1),
                    Email = "max@test.de"
                });

                await db.SaveChangesAsync();
            }
            // Standard Admin erstellen
            if (!db.Users.Any())
            {
                await authService.RegisterUserAsync("admin", "admin123", Roles.Administrator);
            }
        }
        try
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            // LOGIN WINDOW STARTEN
            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
            MainWindow = loginWindow;
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
            LogError(ex);
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

    private static void LogError(Exception ex)
    {
        try
        {
            var logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            Directory.CreateDirectory(logDir);

            var logPath = Path.Combine(logDir, "app.log");
            File.AppendAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");
        }
        catch
        {
            // Logging darf nie crashen
        }
    }
}