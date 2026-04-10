using System.Windows;
using MessageBox = System.Windows.MessageBox;
namespace Praxis.Client.Views;

/// <summary>
/// Dieses Fenster ermöglicht es dem Benutzer,
/// sein Passwort zu ändern.
/// 
/// Der Benutzer muss:
/// - das alte Passwort eingeben
/// - ein neues Passwort eingeben
/// - das neue Passwort bestätigen
/// </summary>
public partial class ChangePasswordWindow : Window
{
    /// <summary>
    /// Enthält das eingegebene alte Passwort.
    /// Wird nach dem Schließen des Fensters ausgelesen.
    /// </summary>
    public string? OldPassword { get; private set; }

    /// <summary>
    /// Enthält das neue Passwort.
    /// Wird nach erfolgreicher Eingabe gesetzt.
    /// </summary>
    public string? NewPassword { get; private set; }

    /// <summary>
    /// Konstruktor des Fensters.
    /// Initialisiert die Benutzeroberfläche.
    /// </summary>
    public ChangePasswordWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Bricht den Vorgang ab und schließt das Fenster ohne Änderungen.
    /// </summary>
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Prüft die eingegebenen Passwörter und speichert sie,
    /// wenn alle Bedingungen erfüllt sind.
    /// 
    /// Prüfungen:
    /// - altes Passwort darf nicht leer sein
    /// - neues Passwort darf nicht leer sein
    /// - neues Passwort muss mindestens 6 Zeichen lang sein
    /// - neues Passwort und Bestätigung müssen übereinstimmen
    /// </summary>
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Eingaben aus den Passwortfeldern lesen und trimmen
        var oldPassword = OldPasswordBox.Password?.Trim() ?? "";
        var newPassword = NewPasswordBox.Password?.Trim() ?? "";
        var confirmPassword = ConfirmPasswordBox.Password?.Trim() ?? "";

        // Altes Passwort prüfen
        if (string.IsNullOrWhiteSpace(oldPassword))
        {
            MessageBox.Show("Bitte altes Passwort eingeben.");
            return;
        }

        // Neues Passwort prüfen
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            MessageBox.Show("Bitte neues Passwort eingeben.");
            return;
        }

        // Mindestlänge prüfen
        if (newPassword.Length < 6)
        {
            MessageBox.Show("Das neue Passwort muss mindestens 6 Zeichen lang sein.");
            return;
        }

        // Vergleich mit Bestätigung
        if (newPassword != confirmPassword)
        {
            MessageBox.Show("Die neuen Passwörter stimmen nicht überein.");
            return;
        }

        // Werte speichern
        OldPassword = oldPassword;
        NewPassword = newPassword;

        // Fenster erfolgreich schließen
        DialogResult = true;
        Close();
    }
}