using System;
using System.Windows;
using System.Windows.Controls;
using Praxis.Client.Session;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;

namespace Praxis.Client.Views.Pages.UserManagement
{
    public partial class EditUserPage : System.Windows.Controls.UserControl
    {
        private User? _currentUser;

        public EditUserPage()
        {
            InitializeComponent();
            Loaded += EditUserPage_Loaded;
        }

        private void EditUserPage_Loaded(object sender, RoutedEventArgs e)
        {
            RoleComboBox.Items.Clear();
            RoleComboBox.Items.Add(Roles.Administrator);
            RoleComboBox.Items.Add(Roles.Mitarbeiter);
            RoleComboBox.Items.Add(Roles.Arzt);
        }

        public void SetUser(User user)
        {
            _currentUser = user;

            UsernameTextBox.Text = user.Username;
            RoleComboBox.SelectedItem = user.Role;
            IsActiveCheckBox.IsChecked = user.IsActive;

            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentUser == null)
                {
                    System.Windows.MessageBox.Show("Kein Benutzer geladen.");
                    return;
                }

                if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                    return;

                var selectedRole = RoleComboBox.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(selectedRole))
                {
                    System.Windows.MessageBox.Show("Bitte eine Rolle auswählen.");
                    return;
                }

                await mainWindow.UpdateUserRoleAsync(_currentUser.Id, selectedRole);

                var shouldBeActive = IsActiveCheckBox.IsChecked == true;
                if (_currentUser.IsActive != shouldBeActive)
                {
                    await mainWindow.ToggleUserActiveAsync(_currentUser.Id);
                }

                var newPassword = NewPasswordBox.Password;
                var confirmPassword = ConfirmPasswordBox.Password;

                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    if (newPassword != confirmPassword)
                    {
                        System.Windows.MessageBox.Show("Die Passwörter stimmen nicht überein.");
                        return;
                    }

                    await mainWindow.ResetUserPasswordAsync(_currentUser.Id, newPassword);
                }

                await mainWindow.OpenUserManagementPageAsync();

                System.Windows.MessageBox.Show("Benutzer wurde aktualisiert.",
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message,
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                await mainWindow.OpenUserManagementPageAsync();
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentUser == null)
                {
                    System.Windows.MessageBox.Show("Kein Benutzer geladen.");
                    return;
                }

                if (_currentUser.Username == UserSession.CurrentUser?.Username)
                {
                    System.Windows.MessageBox.Show("Du kannst dich nicht selbst löschen.",
                        "Sicherheit", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                    return;

                var result = System.Windows.MessageBox.Show(
                    $"Benutzer '{_currentUser.Username}' wirklich löschen?",
                    "Bestätigung",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                await mainWindow.DeleteUserAsync(_currentUser.Id);
                await mainWindow.OpenUserManagementPageAsync();

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