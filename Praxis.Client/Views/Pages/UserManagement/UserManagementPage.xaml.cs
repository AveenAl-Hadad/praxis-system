using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualBasic;
using Praxis.Client.Session;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;

namespace Praxis.Client.Views.Pages.UserManagement
{
    public partial class UserManagementPage : System.Windows.Controls.UserControl
    {
        private List<User> _allUsers = new();

        public UserManagementPage()
        {
            InitializeComponent();
            Loaded += UserManagementPage_Loaded;
        }

        private async void UserManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
        }

        public async Task RefreshAsync()
        {
            await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                    return;

                var users = await mainWindow.GetUsersAsync();
                _allUsers = users.ToList();
                UsersGrid.ItemsSource = _allUsers;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Fehler beim Laden der Benutzer:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SearchUserTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var search = SearchUserTextBox.Text?.Trim().ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(search))
            {
                UsersGrid.ItemsSource = _allUsers;
                return;
            }

            var filtered = _allUsers
                .Where(u =>
                    (u.Username?.ToLower().Contains(search) ?? false) ||
                    (u.Role?.ToLower().Contains(search) ?? false) ||
                    u.Id.ToString().Contains(search))
                .ToList();

            UsersGrid.ItemsSource = filtered;
        }

        private User? GetSelectedUser()
        {
            return UsersGrid.SelectedItem as User;
        }

        private async void NewUserButton_Click(object sender, RoutedEventArgs e)
        {
            if ( System.Windows.Application.Current.MainWindow is  MainWindow mainWindow)
            {
                mainWindow.OpenAddUserPage();
            }
                   
        }

        private void ChangeRoleButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedUser = GetSelectedUser();

            if (selectedUser == null)
            {
                System.Windows.MessageBox.Show("Bitte zuerst einen Benutzer auswählen.");
                return;
            }

            if ( System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.OpenEditUserPage(selectedUser);
            }
        }

        private async void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedUser = GetSelectedUser();
                if (selectedUser == null)
                {
                    System.Windows.MessageBox.Show("Bitte zuerst einen Benutzer auswählen.");
                    return;
                }

                if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                    return;

                var newPassword = Interaction.InputBox(
                    $"Neues Passwort für {selectedUser.Username}:",
                    "Passwort zurücksetzen",
                    "");

                if (string.IsNullOrWhiteSpace(newPassword))
                    return;

                await mainWindow.ResetUserPasswordAsync(selectedUser.Id, newPassword);

                System.Windows.MessageBox.Show("Passwort wurde zurückgesetzt.",
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message,
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ToggleUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedUser = GetSelectedUser();
                if (selectedUser == null)
                {
                    System.Windows.MessageBox.Show("Bitte zuerst einen Benutzer auswählen.");
                    return;
                }

                if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                    return;

                await mainWindow.ToggleUserActiveAsync(selectedUser.Id);
                await LoadUsersAsync();

                System.Windows.MessageBox.Show("Benutzerstatus wurde geändert.",
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message,
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedUser = GetSelectedUser();

                if (selectedUser == null)
                {
                    System.Windows.MessageBox.Show("Bitte zuerst einen Benutzer auswählen.");
                    return;
                }

                if ( System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                    return;

                // ❌ Admin darf sich selbst nicht löschen
                if (selectedUser.Username == UserSession.CurrentUser?.Username)
                {
                    System.Windows.MessageBox.Show("Du kannst dich nicht selbst löschen!",
                        "Sicherheit", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = System.Windows.MessageBox.Show(
                    $"Benutzer '{selectedUser.Username}' wirklich löschen?",
                    "Bestätigung",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;

                await mainWindow.DeleteUserAsync(selectedUser.Id);

                await LoadUsersAsync();

                System.Windows.MessageBox.Show("Benutzer wurde gelöscht.",
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message,
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}