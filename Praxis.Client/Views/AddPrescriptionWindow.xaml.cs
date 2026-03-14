using System.Windows;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

public partial class AddPrescriptionWindow : Window
{
    private readonly IPatientService _patientService;

    public Prescription? CreatedPrescription { get; private set; }

    public AddPrescriptionWindow(IPatientService patientService)
    {
        InitializeComponent();
        _patientService = patientService;
        Loaded += AddPrescriptionWindow_Loaded;
    }

    private async void AddPrescriptionWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var patients = await _patientService.GetAllPatientsAsync();
        PatientComboBox.ItemsSource = patients.ToList();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (PatientComboBox.SelectedItem is not Patient patient)
        {
            MessageBox.Show("Bitte einen Patienten auswählen.");
            return;
        }

        if (string.IsNullOrWhiteSpace(MedicationBox.Text))
        {
            MessageBox.Show("Bitte Medikament eingeben.");
            return;
        }

        if (string.IsNullOrWhiteSpace(DosageBox.Text))
        {
            MessageBox.Show("Bitte Dosierung eingeben.");
            return;
        }

        if (string.IsNullOrWhiteSpace(DoctorNameBox.Text))
        {
            MessageBox.Show("Bitte Arztname eingeben.");
            return;
        }

        CreatedPrescription = new Prescription
        {
            PatientId = patient.Id,
            IssueDate = DateTime.Now,
            PrescriptionNumber = $"RX-{DateTime.Now:yyyyMMddHHmmss}",
            MedicationName = MedicationBox.Text.Trim(),
            Dosage = DosageBox.Text.Trim(),
            Instructions = InstructionsBox.Text.Trim(),
            DoctorName = DoctorNameBox.Text.Trim()
        };

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}