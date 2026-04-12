using System.Collections.Generic;
using System.Windows;

namespace Praxis.Client.Views
{
    public partial class SelectRoomWindow : Window
    {
        public string? SelectedRoomName { get; private set; }

        public SelectRoomWindow(List<string> roomNames)
        {
            InitializeComponent();
            RoomComboBox.ItemsSource = roomNames;

            if (roomNames.Count > 0)
                RoomComboBox.SelectedIndex = 0;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedRoomName = RoomComboBox.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(SelectedRoomName))
            {
                System.Windows.MessageBox.Show("Bitte einen Raum auswählen.");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}