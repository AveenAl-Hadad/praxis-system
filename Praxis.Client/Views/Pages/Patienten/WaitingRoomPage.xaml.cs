using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services.Interface;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views.Pages.Patienten
{
    public partial class WaitingRoomPage : System.Windows.Controls.UserControl
    {
        private readonly IAppointmentService _appointmentService;

        public WaitingRoomPage(IAppointmentService appointmentService)
        {
            InitializeComponent();
            _appointmentService = appointmentService;
            Loaded += WaitingRoomPage_Loaded;
        }

        private async void WaitingRoomPage_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            var data = await _appointmentService.GetWaitingRoomAppointmentsAsync(DateTime.Today);
            WaitingRoomGrid.ItemsSource = data;
        }

        private Appointment? GetSelected()
        {
            return WaitingRoomGrid.SelectedItem as Appointment;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshAsync();
        }

        private async void CheckInButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelected();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst einen Termin auswählen.");
                return;
            }

            await _appointmentService.CheckInAsync(selected.Id);
            await RefreshAsync();
        }

        private async void MoveRoom1Button_Click(object sender, RoutedEventArgs e)
        {
            await MoveToRoomAsync("Raum 1");
        }

        private async void MoveRoom2Button_Click(object sender, RoutedEventArgs e)
        {
            await MoveToRoomAsync("Raum 2");
        }

        private async Task MoveToRoomAsync(string roomName)
        {
            var selected = GetSelected();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst einen Termin auswählen.");
                return;
            }

            await _appointmentService.MoveToRoomAsync(selected.Id, roomName);
            await RefreshAsync();
        }

        private async void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelected();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst einen Termin auswählen.");
                return;
            }

            await _appointmentService.CompleteAppointmentAsync(selected.Id);
            await RefreshAsync();
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelected();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst einen Termin auswählen.");
                return;
            }

            await _appointmentService.CancelAppointmentAsync(selected.Id);
            await RefreshAsync();
        }
    }
}