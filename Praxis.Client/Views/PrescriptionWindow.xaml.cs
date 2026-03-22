using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Client.Session;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

/// <summary>
/// Dieses Fenster zeigt alle Rezepte an und ermöglicht:
/// - neue Rezepte zu erstellen
/// - Rezepte zu filtern
/// - Rezepte als PDF zu exportieren
/// - Rezepte zu löschen
/// 
/// Zugriff haben nur Administratoren und Ärzte.
/// </summary>
public partial class PrescriptionWindow : Window
{
    /// <summary>
    /// Service für die Verwaltung von Rezepten.
    /// </summary>
    private readonly IPrescriptionService _prescriptionService;

    /// <summary>
    /// Service zum Exportieren von Rezepten als PDF.
    /// </summary>
    private readonly IPrescriptionPdfService _pdfService;

    /// <summary>
    /// ServiceProvider zum Erstellen von Fenstern (Dependency Injection).
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Liste aller geladenen Rezepte.
    /// </summary>
    private List<Prescription> _allPrescriptions = new();

    /// <summary>
    /// Konstruktor des Fensters.
    /// 
    /// Übergibt die benötigten Services und registriert das Loaded-Event,
    /// damit beim Öffnen automatisch Daten geladen werden.
    /// </summary>
    public PrescriptionWindow(
        IPrescriptionService prescriptionService,
        IPrescriptionPdfService pdfService,
        IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _prescriptionService = prescriptionService;
        _pdfService = pdfService;
        _serviceProvider = serviceProvider;

        Loaded += PrescriptionWindow_Loaded;
    }

    /// <summary>
    /// Wird beim Laden des Fensters ausgeführt.
    /// 
    /// - prüft die Benutzerberechtigung
    /// - lädt alle Rezepte
    /// </summary>
    private async void PrescriptionWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CheckAccess();
        await LoadPrescriptionsAsync();
    }

    /// <summary>
    /// Prüft, ob der aktuelle Benutzer Zugriff auf Rezepte hat.
    /// 
    /// Nur Administratoren und Ärzte dürfen dieses Fenster nutzen.
    /// </summary>
    private void CheckAccess()
    {
        if (!UserSession.HasAnyRole(Roles.Administrator, Roles.Arzt))
        {
            MessageBox.Show("Sie haben keine Berechtigung für Rezepte.");
            Close();
        }
    }

    /// <summary>
    /// Lädt alle Rezepte aus der Datenbank
    /// und wendet anschließend den Filter an.
    /// </summary>
    private async Task LoadPrescriptionsAsync()
    {
        _allPrescriptions = await _prescriptionService.GetAllPrescriptionsAsync();
        ApplyFilter();
    }

    /// <summary>
    /// Filtert die Rezeptliste nach Patientennamen.
    /// 
    /// Der eingegebene Text wird mit dem Namen des Patienten verglichen.
    /// </summary>
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

    /// <summary>
    /// Wird aufgerufen, wenn sich der Filter ändert.
    /// Aktualisiert die Anzeige der Rezepte.
    /// </summary>
    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        ApplyFilter();
    }

    /// <summary>
    /// Öffnet das Fenster zum Erstellen eines neuen Rezepts.
    /// 
    /// Nach dem Speichern wird das Rezept in die Datenbank übernommen.
    /// </summary>
    private async void NewPrescription_Click(object sender, RoutedEventArgs e)
    {
        var window = _serviceProvider.GetRequiredService<AddPrescriptionWindow>();
        window.Owner = this;

        if (window.ShowDialog() == true)
        {
            await SavePrescriptionAsync(window.CreatedPrescription);
        }
    }

    /// <summary>
    /// Speichert ein neues Rezept und aktualisiert die Anzeige.
    /// </summary>
    private async Task SavePrescriptionAsync(Prescription? prescription)
    {
        if (prescription == null) return;

        await _prescriptionService.AddPrescriptionAsync(
            prescription,
            UserSession.CurrentUser?.Username ?? "System");

        await LoadPrescriptionsAsync();
        await ((MainWindow)Application.Current.MainWindow).LoadDashboardAsync();
    }

    /// <summary>
    /// Exportiert das ausgewählte Rezept als PDF-Datei.
    /// </summary>
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

    /// <summary>
    /// Löscht das ausgewählte Rezept nach Bestätigung durch den Benutzer.
    /// 
    /// Danach wird die Liste aktualisiert.
    /// </summary>
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