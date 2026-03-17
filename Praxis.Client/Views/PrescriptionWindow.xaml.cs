using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Client.Session;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

public partial class PrescriptionWindow : Window
{
    private readonly IPrescriptionService _prescriptionService;
    private readonly IPrescriptionPdfService _pdfService;
    private readonly IServiceProvider _serviceProvider;

    private List<Prescription> _allPrescriptions = new();

    public PrescriptionWindow(IPrescriptionService prescriptionService,IPrescriptionPdfService pdfService,IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _prescriptionService = prescriptionService;
        _pdfService = pdfService;
        _serviceProvider = serviceProvider;
        Loaded += PrescriptionWindow_Loaded;
    }

    private async void PrescriptionWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CheckAccess();
        await LoadPrescriptionsAsync();
    }
    private void CheckAccess()
    {
        if (!UserSession.HasAnyRole(Roles.Administrator, Roles.Arzt))
        {
            MessageBox.Show("Sie haben keine Berechtigung für Rezepte.");
            Close();
        }
    }
    private async Task LoadPrescriptionsAsync()
    {
        _allPrescriptions = await _prescriptionService.GetAllPrescriptionsAsync();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<Prescription> query = _allPrescriptions;

        var patientFilter = PatientFilterBox.Text?.Trim().ToLower() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(patientFilter))
        {
            query = query.Where(p =>
                p.Patient != null &&
                p.Patient.FullName.ToLower().Contains(patientFilter));
        }

        PrescriptionsGrid.ItemsSource = query.ToList();
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        ApplyFilter();
    }

    private async void NewPrescription_Click(object sender, RoutedEventArgs e)
    {
        var window = _serviceProvider.GetRequiredService<AddPrescriptionWindow>();
        window.Owner = this;

        if (window.ShowDialog() == true)
        {
            await SavePrescriptionAsync(window.CreatedPrescription);
        }
    }

    private async Task SavePrescriptionAsync(Prescription? prescription)
    {
        if (prescription == null) return;

        await _prescriptionService.AddPrescriptionAsync(prescription, UserSession.CurrentUser?.Username ?? "System");
        await LoadPrescriptionsAsync();
        await ((MainWindow)Application.Current.MainWindow).LoadDashboardAsync();
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (PrescriptionsGrid.SelectedItem is not Prescription prescription)
        {
            MessageBox.Show("Bitte zuerst ein Rezept auswählen.");
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PDF Datei (*.pdf)|*.pdf",
            FileName = $"{prescription.PrescriptionNumber}.pdf"
        };

        if (dialog.ShowDialog() == true)
        {
            _pdfService.ExportPrescriptionToPdf(prescription, dialog.FileName);
            MessageBox.Show("Rezept wurde als PDF exportiert.");
        }
    }
    private async void DeletePrescription_Click(object sender, RoutedEventArgs e)
    {
        if (PrescriptionsGrid.SelectedItem is not Prescription prescription)
        {
            MessageBox.Show("Bitte zuerst ein Rezept auswählen.");
            return;
        }

        var result = MessageBox.Show(
            $"Möchten Sie das Rezept '{prescription.PrescriptionNumber}' wirklich löschen?",
            "Rezept löschen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            await _prescriptionService.DeletePrescriptionAsync(
                prescription.Id,
                UserSession.CurrentUser?.Username ?? "System");

            await LoadPrescriptionsAsync();
            await ((MainWindow)Application.Current.MainWindow).LoadDashboardAsync();

            MessageBox.Show("Rezept wurde erfolgreich gelöscht.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Löschen des Rezepts:\n{ex.Message}");
        }
    }
}