using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using Praxis.Infrastructure.Services.Interface;

namespace Praxis.Client.Views;

public partial class AppointmentCalendarWindow : Window
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPatientService _patientService;

    public AppointmentCalendarWindow(
        IAppointmentService appointmentService,
        IPatientService patientService)
    {
        InitializeComponent();
        _appointmentService = appointmentService;
        _patientService = patientService;

        Loaded += AppointmentCalendarWindow_Loaded;
    }

    private async void AppointmentCalendarWindow_Loaded(object sender, RoutedEventArgs e)
    {
        WeekDatePicker.SelectedDate = GetStartOfWeek(DateTime.Today);

        var patients = await _patientService.GetAllPatientsAsync();
        PatientFilterComboBox.ItemsSource = patients.ToList();

        await LoadAppointmentsAsync();
    }

    private DateTime GetStartOfWeek(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    private async Task LoadAppointmentsAsync()
    {
        if (WeekDatePicker.SelectedDate == null)
            return;

        int? patientId = PatientFilterComboBox.SelectedValue is int id ? id : null;
        var appointments = await _appointmentService
            .GetAppointmentsByWeekAndPatientAsync(WeekDatePicker.SelectedDate.Value, patientId);

        CalendarDataGrid.ItemsSource = appointments;
    }

    private async void WeekDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WeekDatePicker.SelectedDate.HasValue)
        {
            WeekDatePicker.SelectedDate = GetStartOfWeek(WeekDatePicker.SelectedDate.Value);
            await LoadAppointmentsAsync();
        }
    }

    private async void PatientFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadAppointmentsAsync();
    }

    private async void ShowAllPatients_Click(object sender, RoutedEventArgs e)
    {
        PatientFilterComboBox.SelectedItem = null;
        await LoadAppointmentsAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadAppointmentsAsync();
    }

    private async void CalendarDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (CalendarDataGrid.SelectedItem is not Appointment selectedAppointment)
            return;

        await OpenEditWindowAsync(selectedAppointment.Id);
    }

    private async void EditAppointment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Appointment appointment)
        {
            await OpenEditWindowAsync(appointment.Id);
        }
    }

    private async Task OpenEditWindowAsync(int appointmentId)
    {
        var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);

        if (appointment == null)
        {
            MessageBox.Show("Termin wurde nicht gefunden.");
            return;
        }

        var editWindow = App.ServiceProvider.GetRequiredService<AddAppointmentWindow>();
        editWindow.Owner = this;
        editWindow.SetAppointmentForEdit(appointment);

        var result = editWindow.ShowDialog();

        if (result == true)
        {
            await LoadAppointmentsAsync();
        }
    }

    private async void DeleteAppointment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not Appointment appointment)
            return;

        var result = MessageBox.Show(
            "Möchten Sie diesen Termin wirklich löschen?",
            "Termin löschen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            await _appointmentService.DeleteAppointmentAsync(appointment.Id);
            MessageBox.Show("Termin wurde gelöscht.");
            await LoadAppointmentsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}