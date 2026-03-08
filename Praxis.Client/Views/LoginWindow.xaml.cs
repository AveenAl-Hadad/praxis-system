using System.Windows;
using Praxis.Client.Session;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

public partial class LoginWindow : Window
{
    private readonly IAuthService _authService;

    public LoginWindow(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var user = await _authService.LoginAsync(
            UsernameTextBox.Text,
            PasswordBox.Password);

        if (user == null)
        {
            MessageBox.Show("Benutzername oder Passwort ist ungültig.");
            return;
        }
        MessageBox.Show($"Login erfolgreich: {user.Username} / {user.Role}");
        UserSession.Login(user);

        DialogResult = true;
        Close();
    }
}