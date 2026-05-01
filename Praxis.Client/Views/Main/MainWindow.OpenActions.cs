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
        private void OpenAppointments_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hier öffnest du AppointmentWindow.");
        }
        private void OpenDocuments_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hier öffnest du DocumentWindow.");
        }
        private void OpenWaitingRoom_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hier öffnest du das Wartezimmer.");
        }
        private void OpenInvoices_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hier öffnest du InvoiceWindow.");
        }
        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hier öffnest du ChangePasswordWindow.");
        }
        private void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hier startest du Backup.");
        }
        private void RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hier startest du Restore.");
        }
    }
}
