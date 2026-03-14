namespace Praxis.Domain.Entities;

public class Prescription
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public DateTime IssueDate { get; set; } = DateTime.Now;

    public string PrescriptionNumber { get; set; } = string.Empty;

    public string MedicationName { get; set; } = string.Empty;

    public string Dosage { get; set; } = string.Empty;

    public string Instructions { get; set; } = string.Empty;

    public string DoctorName { get; set; } = string.Empty;
}