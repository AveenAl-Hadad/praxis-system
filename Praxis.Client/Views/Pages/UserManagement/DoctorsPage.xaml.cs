using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views.Pages.UserManagement;

public partial class DoctorsPage : System.Windows.Controls.UserControl
{
    private readonly IDoctorService _doctorService;
    private readonly IRoomService _roomService;
    private readonly IAppointmentTypeService _appointmentTypeService;

    private Doctor? _selectedDoctor;

    public DoctorsPage(
        IDoctorService doctorService,
        IRoomService roomService,
        IAppointmentTypeService appointmentTypeService)
    {
        InitializeComponent();
        _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
        _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
        _appointmentTypeService = appointmentTypeService ?? throw new ArgumentNullException(nameof(appointmentTypeService));

        Loaded += DoctorsPage_Loaded;
    }

    private async void DoctorsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadRoomsAsync();
        await RefreshAsync();
        await LoadAppointmentTypesForNewDoctorAsync();
    }

    public async Task RefreshAsync()
    {
        DoctorsGrid.ItemsSource = await _doctorService.GetAllAsync();
    }

    private async Task LoadRoomsAsync()
    {
        DefaultRoomComboBox.ItemsSource = await _roomService.GetActiveAsync();
    }

    private async Task LoadAppointmentTypesForNewDoctorAsync()
    {
        var allTypes = await _appointmentTypeService.GetAllAsync();

        var items = allTypes.Select(t => new AppointmentTypeCheckboxItem
        {
            Id = t.Id,
            Name = t.Name,
            IsAssigned = false
        }).ToList();

        AppointmentTypesList.ItemsSource = items;
    }

    private async Task LoadAppointmentTypesAsync(int doctorId)
    {
        var allTypes = await _appointmentTypeService.GetAllAsync();
        var assignedIds = await _doctorService.GetAppointmentTypeIdsForDoctorAsync(doctorId);

        var items = allTypes.Select(t => new AppointmentTypeCheckboxItem
        {
            Id = t.Id,
            Name = t.Name,
            IsAssigned = assignedIds.Contains(t.Id)
        }).ToList();

        AppointmentTypesList.ItemsSource = items;
    }

    private async Task<Doctor?> GetLastSavedDoctorAsync()
    {
        var doctors = await _doctorService.GetAllAsync();
        return doctors.OrderByDescending(d => d.Id).FirstOrDefault();
    }

    private async Task SaveAppointmentTypeAssignmentsAsync(int doctorId)
    {
        if (AppointmentTypesList.ItemsSource is not List<AppointmentTypeCheckboxItem> items)
            return;

        var selectedIds = items
            .Where(i => i.IsAssigned)
            .Select(i => i.Id)
            .ToList();

        await _doctorService.SetDoctorAppointmentTypesAsync(doctorId, selectedIds);
    }

    private void ClearForm()
    {
        _selectedDoctor = null;
        TitleTextBox.Text = string.Empty;
        FirstNameTextBox.Text = string.Empty;
        LastNameTextBox.Text = string.Empty;
        SpecialtyTextBox.Text = string.Empty;
        DefaultRoomComboBox.SelectedIndex = -1;
        IsActiveCheckBox.IsChecked = true;
        AllowOnlineBookingCheckBox.IsChecked = true;
        DoctorsGrid.SelectedItem = null;
    }

    private async void NeuButton_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
        await LoadAppointmentTypesForNewDoctorAsync();
    }

    private async void SpeichernButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var roomName = DefaultRoomComboBox.SelectedValue?.ToString() ?? string.Empty;

            Doctor? savedDoctor;

            if (_selectedDoctor == null)
            {
                var newDoctor = new Doctor
                {
                    Title = TitleTextBox.Text,
                    FirstName = FirstNameTextBox.Text,
                    LastName = LastNameTextBox.Text,
                    Specialty = SpecialtyTextBox.Text,
                    DefaultRoomName = roomName,
                    IsActive = IsActiveCheckBox.IsChecked == true,
                    AllowOnlineBooking = AllowOnlineBookingCheckBox.IsChecked == true
                };

                await _doctorService.AddAsync(newDoctor);
                savedDoctor = await GetLastSavedDoctorAsync();
            }
            else
            {
                _selectedDoctor.Title = TitleTextBox.Text;
                _selectedDoctor.FirstName = FirstNameTextBox.Text;
                _selectedDoctor.LastName = LastNameTextBox.Text;
                _selectedDoctor.Specialty = SpecialtyTextBox.Text;
                _selectedDoctor.DefaultRoomName = roomName;
                _selectedDoctor.IsActive = IsActiveCheckBox.IsChecked == true;
                _selectedDoctor.AllowOnlineBooking = AllowOnlineBookingCheckBox.IsChecked == true;

                await _doctorService.UpdateAsync(_selectedDoctor);
                savedDoctor = _selectedDoctor;
            }

            if (savedDoctor == null)
                throw new InvalidOperationException("Behandler konnte nicht gespeichert werden.");

            await SaveAppointmentTypeAssignmentsAsync(savedDoctor.Id);
            await _doctorService.EnsureDefaultScheduleAsync(savedDoctor.Id);

            await RefreshAsync();
            ClearForm();
            await LoadAppointmentTypesForNewDoctorAsync();

            MessageBox.Show("Behandler wurde gespeichert.", "Erfolg",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoeschenButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedDoctor == null)
        {
            MessageBox.Show("Bitte zuerst einen Behandler auswählen.", "Hinweis",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await _doctorService.DeleteAsync(_selectedDoctor.Id);
            await RefreshAsync();
            ClearForm();
            await LoadAppointmentTypesForNewDoctorAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DoctorsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DoctorsGrid.SelectedItem is not Doctor doctor)
            return;

        _selectedDoctor = doctor;
        TitleTextBox.Text = doctor.Title;
        FirstNameTextBox.Text = doctor.FirstName;
        LastNameTextBox.Text = doctor.LastName;
        SpecialtyTextBox.Text = doctor.Specialty;
        DefaultRoomComboBox.SelectedValue = doctor.DefaultRoomName;
        IsActiveCheckBox.IsChecked = doctor.IsActive;
        AllowOnlineBookingCheckBox.IsChecked = doctor.AllowOnlineBooking;

        await LoadAppointmentTypesAsync(doctor.Id);
    }

    public class AppointmentTypeCheckboxItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsAssigned { get; set; }
    }
}