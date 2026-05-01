using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;


namespace Praxis.Client.Views.Main
{
    public partial class MainWindow
    {
        private void SetActiveBottomButton(Button btn)
        {
            if (_activeBottomButton != null)
                _activeBottomButton.Tag = null;

            _activeBottomButton = btn;
            _activeBottomButton.Tag = "Active";
        }

        private void SetInitialBottomButton()
        {
            if (BottomPatientsButton != null)
                SetActiveBottomButton(BottomPatientsButton);
        }

        private void BottomPatients_Click(object sender, RoutedEventArgs e)
        {
            SetActiveBottomButton((Button)sender);
            SwitchModule(BottomModule.Patienten);
        }

        private void BottomLabor_Click(object sender, RoutedEventArgs e)
        {
            SetActiveBottomButton((Button)sender);
            SwitchModule(BottomModule.Labor);
        }

        private void BottomBilling_Click(object sender, RoutedEventArgs e)
        {
            SetActiveBottomButton((Button)sender);
            SwitchModule(BottomModule.Abrechnung);
        }

        private void BottomReports_Click(object sender, RoutedEventArgs e)
        {
            SetActiveBottomButton((Button)sender);
            SwitchModule(BottomModule.Auswertungen);
        }

        private void BottomMessages_Click(object sender, RoutedEventArgs e)
        {
            SetActiveBottomButton((Button)sender);
            SwitchModule(BottomModule.Nachrichten);
        }

        private void BottomCatalogs_Click(object sender, RoutedEventArgs e)
        {
            SetActiveBottomButton((Button)sender);
            SwitchModule(BottomModule.Kataloge);
        }

        private void BottomSetup_Click(object sender, RoutedEventArgs e)
        {
            SetActiveBottomButton((Button)sender);
            SwitchModule(BottomModule.Einrichtung);
        }

        private void BottomSettings_Click(object sender, RoutedEventArgs e)
        {
            SetActiveBottomButton((Button)sender);
            SwitchModule(BottomModule.Einstellungen);
        }
        private async void OpenCatalogCategory(string category)
        {
            try
            {
                LoadPage(_catalogsPage);
                await _catalogsPage.SelectCategoryAsync(category);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Katalog Fehler");
            }
        }

        private async Task<int> GetUnreadMessagesCountAsync()
        {
            return await _messageService.GetUnreadCountAsync("MFA");
        }
        private async Task<string> GetMessagesButtonTextAsync()
        {
            var unreadCount = await _messageService.GetUnreadCountAsync("MFA");

            return unreadCount > 0
                ? $"🔴 Nachrichten ({unreadCount})"
                : "Nachrichten";
        }
    }
}
