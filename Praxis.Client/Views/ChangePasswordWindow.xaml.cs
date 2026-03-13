using System.Windows;

namespace Praxis.Client.Views;

public partial class ChangePasswordWindow : Window
{
    public string? OldPassword { get; private set; }
    public string? NewPassword { get; private set; }

    public ChangePasswordWindow()
    {
        InitializeComponent();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var oldPassword = OldPasswordBox.Password?.Trim() ?? "";
        var newPassword = NewPasswordBox.Password?.Trim() ?? "";
        var confirmPassword = ConfirmPasswordBox.Password?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(oldPassword))
        {
            MessageBox.Show("Bitte altes Passwort eingeben.");
            return;
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            MessageBox.Show("Bitte neues Passwort eingeben.");
            return;
        }

        if (newPassword.Length < 6)
        {
            MessageBox.Show("Das neue Passwort muss mindestens 6 Zeichen lang sein.");
            return;
        }

        if (newPassword != confirmPassword)
        {
            MessageBox.Show("Die neuen Passwörter stimmen nicht überein.");
            return;
        }

        OldPassword = oldPassword;
        NewPassword = newPassword;

        DialogResult = true;
        Close();
    }
}