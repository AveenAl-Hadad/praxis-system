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
        try
        {
            var user = await _authService.LoginAsync(
                UsernameTextBox.Text,
                PasswordBox.Password);

            if (user == null)
            {
                MessageBox.Show("Benutzername oder Passwort ist ungültig.");
                return;
            }

            UserSession.Login(user);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Login-Fehler:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}