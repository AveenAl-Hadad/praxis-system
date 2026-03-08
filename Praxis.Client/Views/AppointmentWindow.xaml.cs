using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services.Interface;

namespace Praxis.Client.Views;

public partial class AppointmentWindow : Window
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentWindow(IAppointmentService appointmentService)
    {
        InitializeComponent();
        _appointmentService = appointmentService;

        Loaded += AppointmentWindow_Loaded;
    }

    private async void AppointmentWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAppointmentsAsync();
    }

    private async Task LoadAppointmentsAsync()
    {
        var appointments = await _appointmentService.GetAllAppointmentsAsync();
        AppointmentsDataGrid.ItemsSource = appointments;
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadAppointmentsAsync();
    }

    private async void NewAppointmentButton_Click(object sender, RoutedEventArgs e)
    {
        var addWindow = App.ServiceProvider.GetRequiredService<AddAppointmentWindow>();
        addWindow.Owner = this;

        var result = addWindow.ShowDialog();

        if (result == true)
        {
            await LoadAppointmentsAsync();
        }
    }
    private async void EditAppointmentButton_Click(object sender, RoutedEventArgs e)
    {
        if (AppointmentsDataGrid.SelectedItem is not Appointment selectedAppointment)
        {
            MessageBox.Show("Bitte zuerst einen Termin auswählen.");
            return;
        }

        var appointmentToEdit = await _appointmentService.GetAppointmentByIdAsync(selectedAppointment.Id);

        if (appointmentToEdit == null)
        {
            MessageBox.Show("Termin wurde nicht gefunden.");
            return;
        }

        var editWindow = App.ServiceProvider.GetRequiredService<AddAppointmentWindow>();
        editWindow.Owner = this;
        editWindow.SetAppointmentForEdit(appointmentToEdit);

        var result = editWindow.ShowDialog();

        if (result == true)
        {
            await LoadAppointmentsAsync();
        }
    }
    private async void DeleteAppointmentButton_Click(object sender, RoutedEventArgs e)
    {
        if (AppointmentsDataGrid.SelectedItem is not Appointment selectedAppointment)
        {
            MessageBox.Show("Bitte zuerst einen Termin auswählen.");
            return;
        }

        var result = MessageBox.Show(
            "Möchten Sie diesen Termin wirklich löschen?",
            "Termin löschen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            await _appointmentService.DeleteAppointmentAsync(selectedAppointment.Id);

            MessageBox.Show("Termin wurde gelöscht.");

            await LoadAppointmentsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

}