using System.Windows;
using System.Windows.Controls;
using Praxis.Client.Session;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

/// <summary>
/// Dieses Fenster ermöglicht dem Benutzer die Anmeldung im System.
/// 
/// Funktionen:
/// - Benutzername und Passwort eingeben
/// - Passwort anzeigen/verbergen
/// - Login durchführen
/// - Hinweis bei vergessenem Passwort anzeigen
/// </summary>
public partial class LoginWindow : Window
{
    /// <summary>
    /// Service für die Authentifizierung (Login).
    /// </summary>
    private readonly IAuthService _authService;

    /// <summary>
    /// Konstruktor des Fensters.
    /// Initialisiert die Benutzeroberfläche und den AuthService.
    /// </summary>
    public LoginWindow(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    /// <summary>
    /// Wird ausgelöst, wenn der Benutzer auf "Login" klickt.
    /// 
    /// Ablauf:
    /// - Benutzername und Passwort auslesen
    /// - Login über den AuthService durchführen
    /// - bei Erfolg Benutzer in die Session speichern
    /// - Fenster schließen
    /// </summary>
    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Passwort je nach Anzeige-Modus holen
            var password = ShowPasswordCheckBox.IsChecked == true
                            ? VisiblePasswordTextBox.Text
                            : PasswordBox.Password;

            // Login durchführen
            var user = await _authService.LoginAsync(
                            UsernameTextBox.Text,
                            PasswordBox.Password);

            // Falls Login fehlschlägt
            if (user == null)
            {
                MessageBox.Show("Benutzername oder Passwort ist ungültig.");
                return;
            }

            // Benutzer in Session speichern
            UserSession.Login(user);

            // Fenster erfolgreich schließen
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

    /// <summary>
    /// Wird ausgelöst, wenn die Checkbox "Passwort anzeigen" geändert wird.
    /// 
    /// Schaltet zwischen:
    /// - PasswordBox (versteckt)
    /// - TextBox (sichtbar)
    /// </summary>
    private void ShowPasswordCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        bool showPassword = ShowPasswordCheckBox.IsChecked == true;

        if (showPassword)
        {
            // Passwort sichtbar anzeigen
            VisiblePasswordTextBox.Text = PasswordBox.Password;
            VisiblePasswordTextBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            // Passwort wieder verstecken
            PasswordBox.Password = VisiblePasswordTextBox.Text;
            PasswordBox.Visibility = Visibility.Visible;
            VisiblePasswordTextBox.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Synchronisiert die PasswordBox mit der sichtbaren TextBox.
    /// </summary>
    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (VisiblePasswordTextBox.Visibility == Visibility.Visible)
            VisiblePasswordTextBox.Text = PasswordBox.Password;
    }

    /// <summary>
    /// Synchronisiert die sichtbare TextBox mit der PasswordBox.
    /// </summary>
    private void VisiblePasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (PasswordBox.Visibility == Visibility.Visible)
            return;

        PasswordBox.Password = VisiblePasswordTextBox.Text;
    }

    /// <summary>
    /// Zeigt eine Information an, wenn der Benutzer sein Passwort vergessen hat.
    /// </summary>
    private void ForgotPassword_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Falls Sie Ihr Passwort vergessen haben, wenden Sie sich bitte an den Administrator oder die Praxisleitung.",
            "Passwort vergessen",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}