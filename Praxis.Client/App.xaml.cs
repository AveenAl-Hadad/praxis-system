using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Praxis.Domain.Entities;
using Praxis.Infrastructure;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;

namespace Praxis.Client;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        this.DispatcherUnhandledException += (s, exArgs) =>
        {
            LogError(exArgs.Exception);
            MessageBox.Show(
                "Ein unerwarteter Fehler ist aufgetreten.\nDetails stehen in logs\\app.log",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            exArgs.Handled = true; // verhindert App-Crash
        };

        AppDomain.CurrentDomain.UnhandledException += (s, exArgs) =>
        {
            if (exArgs.ExceptionObject is Exception ex)
                LogError(ex);
        };

        TaskScheduler.UnobservedTaskException += (s, exArgs) =>
        {
            LogError(exArgs.Exception);
            exArgs.SetObserved();
        };

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
                services.AddScoped<PatientService>();

                // MainWindow über DI
                services.AddTransient<MainWindow>();
            })
            .Build();

        // DB erstellen + Testdaten einfügen
        using (var scope = _host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PraxisDbContext>();

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
        }

        // UI starten
        _host.Services.GetRequiredService<MainWindow>().Show();
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