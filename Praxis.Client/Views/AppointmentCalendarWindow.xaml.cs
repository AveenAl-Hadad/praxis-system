using System.Windows;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

public partial class AppointmentCalendarWindow : Window
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentCalendarWindow(IAppointmentService appointmentService)
    {
        InitializeComponent();
        _appointmentService = appointmentService;

        Loaded += AppointmentCalendarWindow_Loaded;
    }

    private async void AppointmentCalendarWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CalendarDatePicker.SelectedDate = DateTime.Today;
        await LoadAppointmentsAsync(DateTime.Today);
    }

    private async void CalendarDatePicker_SelectedDateChanged(object sender, RoutedEventArgs e)
    {
        if (CalendarDatePicker.SelectedDate.HasValue)
        {
            await LoadAppointmentsAsync(CalendarDatePicker.SelectedDate.Value);
        }
    }

    private async Task LoadAppointmentsAsync(DateTime date)
    {
        var appointments = await _appointmentService.GetAppointmentsByDateAsync(date);
        CalendarAppointmentsGrid.ItemsSource = appointments;
    }
}