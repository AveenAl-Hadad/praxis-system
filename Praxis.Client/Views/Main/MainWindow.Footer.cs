using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views.Main
{
    public partial class MainWindow
    {
        private async Task RefreshBottomStatusAsync()
        {
            var tasks = (await GetOpenDashboardTasksAsync()).ToList();

            var openCount = tasks.Count;
            var overdueCount = tasks.Count(t =>
                t.DueDate != null &&
                t.DueDate.Value.Date < DateTime.Today);

            OpenTasksButton.Content = openCount.ToString();
            OverdueTasksButton.Content = overdueCount.ToString();

            SystemStatusButton.Content = "✓";
        }
        private async void OpenTasksButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchModule(BottomModule.Patienten);
            LoadPage(_dashboardPage);
            await RefreshBottomStatusAsync();
        }
        private async void OverdueTasksButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchModule(BottomModule.Patienten);
            LoadPage(_dashboardPage);
            await RefreshBottomStatusAsync();

            MessageBox.Show("Überfällige Aufgaben werden im Dashboard angezeigt.");
        }
        private async void SystemStatusButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshBottomStatusAsync();
            MessageBox.Show("Systemstatus aktualisiert.");
        }
    }
}
