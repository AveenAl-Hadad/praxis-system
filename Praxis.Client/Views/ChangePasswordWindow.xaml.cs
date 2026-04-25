using Praxis.Application.Interfaces;
using Praxis.Client.Session;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly IAuthService _authService;

    public ChangePasswordWindow(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var user = UserSession.CurrentUser;

            if (user == null)
            {
                MessageBox.Show("Kein Benutzer angemeldet.");
                return;
            }

            var oldPassword = OldPasswordBox.Password;
            var newPassword = NewPasswordBox.Password;
            var confirmPassword = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(oldPassword))
            {
                MessageBox.Show("Bitte altes Passwort eingeben.");
                return;
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                MessageBox.Show("Das neue Passwort muss mindestens 6 Zeichen haben.");
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Die neuen Passwörter stimmen nicht überein.");
                return;
            }

            await _authService.ChangePasswordAsync(user.Id, oldPassword, newPassword);

            MessageBox.Show("Passwort wurde geändert.");
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler");
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}