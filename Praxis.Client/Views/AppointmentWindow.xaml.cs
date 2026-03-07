using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Infrastructure.Services;

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
}