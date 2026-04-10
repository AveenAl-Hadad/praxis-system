using System.Windows;
using Praxis.Application.Interfaces;


namespace Praxis.Client.Views;

/// <summary>
/// Dieses Fenster zeigt das Audit-Log des Systems.
/// 
/// Im Audit-Log werden wichtige Aktionen gespeichert,
/// z. B.:
/// - Benutzeraktionen
/// - Änderungen an Daten
/// - Systemereignisse
/// 
/// Die Daten werden in einer Tabelle (DataGrid) angezeigt.
/// </summary>
public partial class AuditLogWindow : Window
{
    /// <summary>
    /// Service zum Laden der Audit-Logs aus der Datenbank.
    /// </summary>
    private readonly IAuditService _auditService;

    /// <summary>
    /// Konstruktor des Fensters.
    /// 
    /// Initialisiert die UI und lädt beim Öffnen automatisch die Logs.
    /// </summary>
    /// <param name="auditService">Service für Audit-Logs.</param>
    public AuditLogWindow(IAuditService auditService)
    {
        InitializeComponent();
        _auditService = auditService;

        // Event wird ausgelöst, wenn das Fenster geladen ist
        Loaded += Window_Loaded;
    }

    /// <summary>
    /// Wird beim Laden des Fensters ausgeführt.
    /// 
    /// Lädt alle Audit-Logs und zeigt sie im DataGrid an.
    /// </summary>
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var logs = await _auditService.GetLogsAsync();

        // Logs im Grid anzeigen
        AuditGrid.ItemsSource = logs;
    }
}