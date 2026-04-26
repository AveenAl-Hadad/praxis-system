namespace Praxis.Domain.Entities;

public class PatientCase
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public string CaseNumber { get; set; } = string.Empty;
    public string InsuranceType { get; set; } = "GKV"; // GKV, PKV, BG, Selbstzahler
    public string InsuranceName { get; set; } = string.Empty;
    public string Quarter { get; set; } = string.Empty; // z.B. 2026-Q2

    public DateTime ValidFrom { get; set; } = DateTime.Today;
    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; } = true;

    public string Notes { get; set; } = string.Empty;
}