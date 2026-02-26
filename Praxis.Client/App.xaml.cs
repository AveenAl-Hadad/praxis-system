using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Praxis.Infrastructure;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Client;

public partial class App : System.Windows.Application
{
    private IHost? _host;

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

                MessageBox.Show($"DB Pfad:\n{dbPath}");

                services.AddInfrastructure(dbPath);
                services.AddSingleton<MainWindow>();
            })
            .Build();

        // Datenbank-Datei/Schema anlegen (leer, ohne Tabellen)
        using (var scope = _host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PraxisDbContext>();
            await db.Database.EnsureCreatedAsync();

            MessageBox.Show("DB wurde erstellt ✅");
        }

        _host.Services.GetRequiredService<MainWindow>().Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null) await _host.StopAsync();
        _host?.Dispose();
        base.OnExit(e);
    }
}