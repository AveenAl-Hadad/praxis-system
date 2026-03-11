using System;
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

                services.AddScoped<IPatientService, PatientService>();
                services.AddScoped<IAppointmentService, AppointmentService>();
                services.AddScoped<IPasswordService, PasswordService>();
                services.AddScoped<IAuthService, AuthService>();
                services.AddScoped<IUserManagementService, UserManagementService>();


                services.AddTransient<MainWindow>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<WaitingRoomWindow>();
                services.AddTransient<AddAppointmentWindow>();
                services.AddTransient<AppointmentWindow>();
                services.AddTransient<AppointmentCalendarWindow>();
                services.AddTransient<AddPatientWindow>();
                services.AddTransient<UserManagementWindow>();
                ServiceProvider = services.BuildServiceProvider();
            })
            .Build();

        using (var scope = _host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PraxisDbContext>();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

            await db.Database.EnsureCreatedAsync();

            if (!db.Patients.Any())
            {
                db.Patients.Add(new Patient
                {
                    Vorname = "Max",
                    Nachname = "Mustermann",
                    Geburtsdatum = new DateTime(1980, 1, 1),
                    Email = "max@test.de",
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