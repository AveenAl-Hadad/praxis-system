using Praxis.Client.Security;
using Praxis.Client.Session;
using System.Windows;
using Praxis.Application.Interfaces;
using Praxis.Client.Views;

using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views.Pages;

public partial class SettingsPage : System.Windows.Controls.UserControl
{
    private readonly IAuthService _authService;
    public SettingsPage(IAuthService authService)
    {
        InitializeComponent();

        _authService = authService;

        var user = UserSession.CurrentUser;

        CurrentUserText.Text = user == null
            ? "Nicht angemeldet"
            : $"Angemeldet als: {user.Username} | Rolle: {user.Role}";

        AdminPanel.Visibility = PermissionHelper.IsAdmin
            ? Visibility.Visible
            : Visibility.Collapsed;
      
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
        MessageBox.Show("Helles Design aktiviert.");
    }

    private void DarkTheme_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Dunkles Design aktiviert.");
    }

    private void UserManagement_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Benutzerverwaltung öffnen.");
    }

    private void CreateBackup_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Backup erstellen.");
    }

    private void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Backup wiederherstellen.");
    }
}