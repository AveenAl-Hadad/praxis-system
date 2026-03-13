using System.Windows;
using System.Windows.Controls;
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
            var password = ShowPasswordCheckBox.IsChecked == true
                            ? VisiblePasswordTextBox.Text
                            : PasswordBox.Password;

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
    private void ShowPasswordCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        bool showPassword = ShowPasswordCheckBox.IsChecked == true;

        if (showPassword)
        {
            VisiblePasswordTextBox.Text = PasswordBox.Password;
            VisiblePasswordTextBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            PasswordBox.Password = VisiblePasswordTextBox.Text;
            PasswordBox.Visibility = Visibility.Visible;
            VisiblePasswordTextBox.Visibility = Visibility.Collapsed;
        }
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (VisiblePasswordTextBox.Visibility == Visibility.Visible)
            VisiblePasswordTextBox.Text = PasswordBox.Password;
    }

    private void VisiblePasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (PasswordBox.Visibility == Visibility.Visible)
            return;

        PasswordBox.Password = VisiblePasswordTextBox.Text;
    }

    private void ForgotPassword_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Falls Sie Ihr Passwort vergessen haben, wenden Sie sich bitte an den Administrator oder die Praxisleitung.",
            "Passwort vergessen",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}