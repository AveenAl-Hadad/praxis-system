using System.Windows;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

/// <summary>
/// Dieses Fenster dient zum Erstellen eines neuen Rezepts.
/// 
/// Der Benutzer kann:
/// - einen Patienten auswählen
/// - ein Medikament eingeben
/// - die Dosierung festlegen
/// - Anweisungen hinzufügen
/// - den Namen des Arztes angeben
/// 
/// Nach dem Speichern wird ein neues Prescription-Objekt erstellt.
/// </summary>
public partial class AddPrescriptionWindow : Window
{
    /// <summary>
    /// Service zum Laden der Patienten.
    /// </summary>
    private readonly IPatientService _patientService;

    /// <summary>
    /// Enthält das erstellte Rezept nach dem Speichern.
    /// Wird vom aufrufenden Fenster ausgelesen.
    /// </summary>
    public Prescription? CreatedPrescription { get; private set; }

    /// <summary>
    /// Konstruktor des Fensters.
    /// 
    /// Initialisiert die UI und lädt beim Öffnen die Patientenliste.
    /// </summary>
    /// <param name="patientService">Service zum Laden der Patienten.</param>
    public AddPrescriptionWindow(IPatientService patientService)
    {
        InitializeComponent();
        _patientService = patientService;

        // Event wird ausgelöst, wenn das Fenster geladen ist
        Loaded += AddPrescriptionWindow_Loaded;
    }

    /// <summary>
    /// Wird beim Laden des Fensters ausgeführt.
    /// 
    /// Lädt alle Patienten und zeigt sie in der ComboBox an.
    /// </summary>
    private async void AddPrescriptionWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var patients = await _patientService.GetAllPatientsAsync();
        PatientComboBox.ItemsSource = patients.ToList();
    }

    /// <summary>
    /// Speichert das Rezept.
    /// 
    /// Ablauf:
    /// - Prüft alle Eingaben
    /// - Erstellt ein neues Prescription-Objekt
    /// - Schließt das Fenster bei Erfolg
    /// </summary>
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Prüfen, ob ein Patient ausgewählt wurde
        if (PatientComboBox.SelectedItem is not Patient patient)
        {
            MessageBox.Show("Bitte einen Patienten auswählen.");
            return;
        }

        // Medikament prüfen
        if (string.IsNullOrWhiteSpace(MedicationBox.Text))
        {
            MessageBox.Show("Bitte Medikament eingeben.");
            return;
        }

        // Dosierung prüfen
        if (string.IsNullOrWhiteSpace(DosageBox.Text))
        {
            MessageBox.Show("Bitte Dosierung eingeben.");
            return;
        }

        // Arztname prüfen
        if (string.IsNullOrWhiteSpace(DoctorNameBox.Text))
        {
            MessageBox.Show("Bitte Arztname eingeben.");
            return;
        }

        // Neues Rezept erstellen
        CreatedPrescription = new Prescription
        {
            PatientId = patient.Id,
            IssueDate = DateTime.Now,

            // Beispiel: RX-20260322153000
            PrescriptionNumber = $"RX-{DateTime.Now:yyyyMMddHHmmss}",

            MedicationName = MedicationBox.Text.Trim(),
            Dosage = DosageBox.Text.Trim(),
            Instructions = InstructionsBox.Text.Trim(),
            DoctorName = DoctorNameBox.Text.Trim()
        };

        // Fenster erfolgreich schließen
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Bricht den Vorgang ab und schließt das Fenster ohne zu speichern.
    /// </summary>
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}