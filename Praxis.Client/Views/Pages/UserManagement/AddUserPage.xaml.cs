using System;
using System.Windows;
using System.Windows.Controls;
using Praxis.Domain.Constants;

namespace Praxis.Client.Views.Pages.UserManagement
{
    public partial class AddUserPage : System.Windows.Controls.UserControl
    {
        public AddUserPage()
        {
            InitializeComponent();
            Loaded += AddUserPage_Loaded;
        }

        private void AddUserPage_Loaded(object sender, RoutedEventArgs e)
        {
            RoleComboBox.Items.Clear();
            RoleComboBox.Items.Add(Roles.Administrator);
            RoleComboBox.Items.Add(Roles.Mitarbeiter);
            RoleComboBox.Items.Add(Roles.Arzt);
            RoleComboBox.SelectedIndex = 1;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                    return;

                var username = UsernameTextBox.Text?.Trim();
                var password = PasswordBox.Password;
                var role = RoleComboBox.SelectedItem?.ToString() ?? Roles.Mitarbeiter;
                var isActive = IsActiveCheckBox.IsChecked == true;

                if (string.IsNullOrWhiteSpace(username))
                {
                    System.Windows.MessageBox.Show("Bitte Benutzername eingeben.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    System.Windows.MessageBox.Show("Bitte Passwort eingeben.");
                    return;
                }

                var createdUser = await mainWindow.CreateUserAsync(username, password, role);

                if (!isActive)
                {
                    await mainWindow.ToggleUserActiveAsync(createdUser.Id);
                }

                await mainWindow.OpenUserManagementPageAsync();

                System.Windows.MessageBox.Show("Benutzer wurde erfolgreich erstellt.",
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message,
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            UsernameTextBox.Clear();
            PasswordBox.Clear();
            RoleComboBox.SelectedIndex = 1;
            IsActiveCheckBox.IsChecked = true;
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                await mainWindow.OpenUserManagementPageAsync();
            }
        }
    }
}