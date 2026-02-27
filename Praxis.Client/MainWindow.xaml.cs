using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client;

public partial class MainWindow : Window
{
    private readonly PatientService _patientService;

    public MainWindow(PatientService patientService)
    {
        InitializeComponent();
        _patientService = patientService;

        Loaded += async (_, __) => await LoadPatientsAsync();
    }

    private async Task LoadPatientsAsync()
    {
        try
        {
            StatusText.Text = "Lade Patienten...";
            List<Patient> patients = await _patientService.GetAllPatientsAsync();
            PatientsGrid.ItemsSource = patients;
            StatusText.Text = $"Anzahl Patienten: {patients.Count}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Fehler beim Laden.";
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadPatientsAsync();
    }
}