using System.Windows;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services.Interface;

namespace Praxis.Client.Views;

public partial class WaitingRoomWindow : Window
{
    private readonly IAppointmentService _appointmentService;

    public WaitingRoomWindow(IAppointmentService appointmentService)
    {
        InitializeComponent();
        _appointmentService = appointmentService;
        Loaded += WaitingRoomWindow_Loaded;
    }

    private async void WaitingRoomWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadWaitingRoomAsync();
    }

    private async Task LoadWaitingRoomAsync()
    {
        var appointments = await _appointmentService.GetWaitingRoomAppointmentsAsync(DateTime.Today);
        WaitingRoomDataGrid.ItemsSource = appointments;
        InfoTextBlock.Text = $"Wartezimmer für {DateTime.Today:dd.MM.yyyy} – {appointments.Count} Einträge";
    }

    private async Task UpdateSelectedStatusAsync(string newStatus)
    {
        if (WaitingRoomDataGrid.SelectedItem is not Appointment selectedAppointment)
        {
            MessageBox.Show("Bitte zuerst einen Termin auswählen.");
            return;
        }

        try
        {
            await _appointmentService.UpdateAppointmentStatusAsync(selectedAppointment.Id, newStatus);
            await LoadWaitingRoomAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadWaitingRoomAsync();
    }

    private async void CheckInButton_Click(object sender, RoutedEventArgs e)
    {
        await UpdateSelectedStatusAsync("Im Wartezimmer");
    }

    private async void StartTreatmentButton_Click(object sender, RoutedEventArgs e)
    {
        await UpdateSelectedStatusAsync("In Behandlung");
    }

    private async void CompleteButton_Click(object sender, RoutedEventArgs e)
    {
        await UpdateSelectedStatusAsync("Erledigt");
    }

    private async void NoShowButton_Click(object sender, RoutedEventArgs e)
    {
        await UpdateSelectedStatusAsync("Nicht erschienen");
    }
}