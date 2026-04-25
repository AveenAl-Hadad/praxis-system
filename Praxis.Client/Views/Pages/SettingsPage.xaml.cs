using Praxis.Client.Security;
using Praxis.Client.Session;
using System.Windows;
using Praxis.Application.Interfaces;
using Praxis.Client.Views;
using Praxis.Client.Logic.UI;
using Microsoft.Win32;

using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;


namespace Praxis.Client.Views.Pages;

public partial class SettingsPage : System.Windows.Controls.UserControl
{
    private readonly IAuthService _authService;
    private readonly IThemeService _themeService;
    private readonly IBackupService _backupService;
    public SettingsPage(IAuthService authService,
                        IBackupService backupService,
                        IThemeService themeService)
    {
        InitializeComponent();

        _authService = authService;
        _themeService = themeService;
        _backupService = backupService;

        var user = UserSession.CurrentUser;

        CurrentUserText.Text = user == null
            ? "Nicht angemeldet"
            : $"Angemeldet als: {user.Username} | Rolle: {user.Role}";

        AdminPanel.Visibility = PermissionHelper.IsAdmin
            ? Visibility.Visible
            : Visibility.Collapsed;
        _backupService = backupService;
    }

    private void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        var window = new ChangePasswordWindow(_authService)
        {
            Owner = Window.GetWindow(this)
        };

        window.ShowDialog();
    }
    private void LightTheme_Click(object sender, RoutedEventArgs e)
    {
        _themeService.ApplyLightTheme();
    }

    private void DarkTheme_Click(object sender, RoutedEventArgs e)
    {
        _themeService.ApplyDarkTheme();
    }

    private async void UserManagement_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            await mainWindow.OpenUserManagementPageAsync();
    }

    private async void CreateBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "SQLite Backup (*.db)|*.db",
            FileName = $"praxis-backup-{DateTime.Now:yyyy-MM-dd-HH-mm}.db"
        };

        if (dialog.ShowDialog() != true)
            return;

        await _backupService.CreateBackupAsync(dialog.FileName);
        MessageBox.Show("Backup wurde erstellt.");
    }

    private async void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "SQLite Backup (*.db)|*.db"
        };

        if (dialog.ShowDialog() != true)
            return;

        var confirm = MessageBox.Show(
            "Backup wirklich wiederherstellen? Die aktuelle Datenbank wird überschrieben.",
            "Backup wiederherstellen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
            return;

        await _backupService.RestoreBackupAsync(dialog.FileName);

        MessageBox.Show("Backup wurde wiederhergestellt. Bitte Programm neu starten.");
    }
}