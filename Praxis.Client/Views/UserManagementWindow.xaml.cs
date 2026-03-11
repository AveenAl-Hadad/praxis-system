using System.Windows;
using Praxis.Client.Session;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

public partial class UserManagementWindow : Window
{
    private readonly IUserManagementService _userManagementService;

    public UserManagementWindow(IUserManagementService userManagementService)
    {
        InitializeComponent();
        _userManagementService = userManagementService;

        RoleComboBox.ItemsSource = new[] { Roles.Administrator, Roles.Mitarbeiter };
        EditRoleComboBox.ItemsSource = new[] { Roles.Administrator, Roles.Mitarbeiter };

        RoleComboBox.SelectedIndex = 1;
        EditRoleComboBox.SelectedIndex = 1;

        Loaded += UserManagementWindow_Loaded;
    }

    private async void UserManagementWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (!UserSession.HasRole(Roles.Administrator))
        {
            MessageBox.Show("Nur Administratoren dürfen die Benutzerverwaltung öffnen.");
            Close();
            return;
        }

        await LoadUsersAsync();
    }

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

    private User? GetSelectedUser()
    {
        return UsersGrid.SelectedItem as User;
    }

    private async void CreateUser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var username = UsernameTextBox.Text;
            var password = PasswordBox.Password;
            var role = RoleComboBox.SelectedItem?.ToString() ?? Roles.Mitarbeiter;

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

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}