using System.Windows;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

public partial class AuditLogWindow : Window
{
    private readonly IAuditService _auditService;

    public AuditLogWindow(IAuditService auditService)
    {
        InitializeComponent();
        _auditService = auditService;
        Loaded += Window_Loaded;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var logs = await _auditService.GetLogsAsync();
        AuditGrid.ItemsSource = logs;
    }
}