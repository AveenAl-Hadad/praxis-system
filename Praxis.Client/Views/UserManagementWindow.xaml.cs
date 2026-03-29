using System.Windows;
using Praxis.Client.Session;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

/// <summary>
/// Dieses Fenster dient zur Verwaltung von Benutzern.
/// 
/// Funktionen:
/// - Benutzer anzeigen
/// - neue Benutzer erstellen
/// - Rollen ändern
/// - Passwörter zurücksetzen
/// - Benutzer löschen
/// 
/// Zugriff ist nur für Administratoren erlaubt.
/// </summary>
public partial class UserManagementWindow : Window
{
    /// <summary>
    /// Service für alle Benutzerverwaltungsfunktionen.
    /// </summary>
    private readonly IUserManagementService _userManagementService;

    /// <summary>
    /// Konstruktor des Fensters.
    /// 
    /// Initialisiert die Rollen-Auswahl und lädt beim Öffnen
    /// automatisch alle Benutzer.
    /// </summary>
    public UserManagementWindow(IUserManagementService userManagementService)
    {
        InitializeComponent();
        _userManagementService = userManagementService;

        // Rollen für Auswahlboxen festlegen
        RoleComboBox.ItemsSource = new[] { Roles.Administrator, Roles.Mitarbeiter, Roles.Arzt };
        EditRoleComboBox.ItemsSource = new[] { Roles.Administrator, Roles.Mitarbeiter, Roles.Arzt };

        // Standardauswahl setzen
        RoleComboBox.SelectedIndex = 1;
        EditRoleComboBox.SelectedIndex = 1;

        Loaded += UserManagementWindow_Loaded;
    }

    /// <summary>
    /// Wird beim Laden des Fensters ausgeführt.
    /// 
    /// - prüft die Benutzerberechtigung
    /// - lädt alle Benutzer
    /// </summary>
    private async void UserManagementWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CheckAccess();
        await LoadUsersAsync();
    }

    /// <summary>
    /// Prüft, ob der aktuelle Benutzer Administrator ist.
    /// 
    /// Nur Administratoren dürfen Benutzer verwalten.
    /// </summary>
    private void CheckAccess()
    {
        if (!UserSession.HasRole(Roles.Administrator))
        {
            MessageBox.Show("Nur Administratoren dürfen Benutzer verwalten.");
            Close();
        }
    }

    /// <summary>
    /// Lädt alle Benutzer aus der Datenbank
    /// und zeigt sie im DataGrid an.
    /// </summary>
    private async Task LoadUsersAsync()
    {
        try
        {
            var users = await _userManagementService.GetAllUsersAsync();
            UsersGrid.ItemsSource = users;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim Laden der Benutzer:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Gibt den aktuell ausgewählten Benutzer zurück.
    /// </summary>
    private User? GetSelectedUser()
    {
        return UsersGrid.SelectedItem as User;
    }

    /// <summary>
    /// Erstellt einen neuen Benutzer.
    /// 
    /// Die eingegebenen Daten werden an den Service übergeben
    /// und anschließend die Liste aktualisiert.
    /// </summary>
    private async void CreateUser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var username = UsernameTextBox.Text?.Trim() ?? "";
            var password = PasswordBox.Password?.Trim() ?? "";
            var role = RoleComboBox.SelectedItem?.ToString() ?? Roles.Mitarbeiter;

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Bitte einen Benutzernamen eingeben.");
                UsernameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Bitte ein Passwort eingeben.");
                PasswordBox.Focus();
                return;
            }

            await _userManagementService.CreateUserAsync(username, password, role);

            UsernameTextBox.Clear();
            PasswordBox.Clear();
            RoleComboBox.SelectedItem = Roles.Mitarbeiter;

            await LoadUsersAsync();

            MessageBox.Show("Benutzer wurde erfolgreich angelegt.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim Anlegen:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Ändert die Rolle des ausgewählten Benutzers.
    /// </summary>
    private async void ChangeRole_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var user = GetSelectedUser();
            if (user == null)
            {
                MessageBox.Show("Bitte zuerst einen Benutzer auswählen.");
                return;
            }

            var newRole = EditRoleComboBox.SelectedItem?.ToString() ?? Roles.Mitarbeiter;

            await _userManagementService.UpdateUserRoleAsync(user.Id, newRole);
            await LoadUsersAsync();

            MessageBox.Show("Rolle wurde aktualisiert.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim Ändern der Rolle:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Setzt das Passwort eines Benutzers zurück.
    /// 
    /// Ein neues Passwort wird über ein Eingabefeld abgefragt.
    /// </summary>
    private async void ResetPassword_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var user = GetSelectedUser();
            if (user == null)
            {
                MessageBox.Show("Bitte zuerst einen Benutzer auswählen.");
                return;
            }

            var newPassword = Microsoft.VisualBasic.Interaction.InputBox(
                "Neues Passwort eingeben:",
                "Passwort zurücksetzen",
                "Test123!");

            if (string.IsNullOrWhiteSpace(newPassword))
                return;

            await _userManagementService.ResetPasswordAsync(user.Id, newPassword);

            MessageBox.Show("Passwort wurde zurückgesetzt.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim Zurücksetzen des Passworts:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Löscht den ausgewählten Benutzer.
    /// 
    /// Der aktuell angemeldete Benutzer kann nicht gelöscht werden.
    /// </summary>
    private async void DeleteUser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var user = GetSelectedUser();
            if (user == null)
            {
                MessageBox.Show("Bitte zuerst einen Benutzer auswählen.");
                return;
            }

            // Selbstlöschung verhindern
            if (UserSession.CurrentUser != null && user.Id == UserSession.CurrentUser.Id)
            {
                MessageBox.Show("Der aktuell angemeldete Benutzer kann nicht gelöscht werden.");
                return;
            }

            var result = MessageBox.Show(
                $"Benutzer '{user.Username}' wirklich löschen?",
                "Löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            await _userManagementService.DeleteUserAsync(user.Id);
            await LoadUsersAsync();

            MessageBox.Show("Benutzer wurde gelöscht.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler beim Löschen:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Schließt das Fenster.
    /// </summary>
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    /// <summary>
    /// Alias für "Benutzer neu".
    /// </summary>
    private void AddUser_Click(object sender, RoutedEventArgs e)
    {
        CreateUser_Click(sender, e);
    }

    /// <summary>
    /// Alias für "Bearbeiten".
    /// Ändert in diesem Fenster die Rolle des ausgewählten Benutzers.
    /// </summary>
    private void EditUser_Click(object sender, RoutedEventArgs e)
    {
        ChangeRole_Click(sender, e);
    }

    /// <summary>
    /// Alias für "Aktualisieren".
    /// </summary>
    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadUsersAsync();
    }

    private void UsersGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (UsersGrid.SelectedItem is User user)
        {
            SelectedUsernameText.Text = user.Username;
            SelectedUserRoleText.Text = user.Role;
            SelectedUserStatusText.Text = user.IsActive ? "Aktiv" : "Inaktiv";
        }
        else
        {
            SelectedUsernameText.Text = "-";
            SelectedUserRoleText.Text = "-";
            SelectedUserStatusText.Text = "-";
        }
    }
}