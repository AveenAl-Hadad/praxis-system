using Praxis.Client.Security;
using Praxis.Client.Session;
using System.Windows;
using Praxis.Application.Interfaces;
using Praxis.Client.Views;
using Praxis.Client.Logic.UI;
using Microsoft.Win32;
using Praxis.Domain.Entities;

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
    private readonly IPracticeSettingsService _practiceSettingsService;
    private PracticeSettings? _practiceSettings;
    public SettingsPage(
    IAuthService authService,
    IThemeService themeService,
    IBackupService backupService,
    IPracticeSettingsService practiceSettingsService)
    {
        InitializeComponent();

        _authService = authService;
        _themeService = themeService;
        _backupService = backupService;
        _practiceSettingsService = practiceSettingsService;

        var user = UserSession.CurrentUser;

        CurrentUserText.Text = user == null
            ? "Nicht angemeldet"
            : $"Angemeldet als: {user.Username} | Rolle: {user.Role}";

        AdminPanel.Visibility = PermissionHelper.IsAdmin
            ? Visibility.Visible
            : Visibility.Collapsed;

        PracticeSettingsPanel.Visibility = PermissionHelper.IsAdmin
            ? Visibility.Visible
            : Visibility.Collapsed;

        Loaded += async (_, _) => await LoadPracticeSettingsAsync();
    }

    private async Task LoadPracticeSettingsAsync()
    {
        _practiceSettings = await _practiceSettingsService.GetAsync();

        PracticeNameBox.Text = _practiceSettings.PracticeName;
        DoctorNameBox.Text = _practiceSettings.DoctorName;
        StreetBox.Text = _practiceSettings.Street;
        ZipCityBox.Text = _practiceSettings.ZipCity;
        PhoneBox.Text = _practiceSettings.Phone;
        EmailBox.Text = _practiceSettings.Email;
    }

    private async void SavePracticeSettings_Click(object sender, RoutedEventArgs e)
    {
        if (_practiceSettings == null)
            _practiceSettings = new PracticeSettings();

        _practiceSettings.PracticeName = PracticeNameBox.Text;
        _practiceSettings.DoctorName = DoctorNameBox.Text;
        _practiceSettings.Street = StreetBox.Text;
        _practiceSettings.ZipCity = ZipCityBox.Text;
        _practiceSettings.Phone = PhoneBox.Text;
        _practiceSettings.Email = EmailBox.Text;

        await _practiceSettingsService.SaveAsync(_practiceSettings);

        MessageBox.Show("Praxisdaten wurden gespeichert.");
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