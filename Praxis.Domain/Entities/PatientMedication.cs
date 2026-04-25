namespace Praxis.Domain.Entities;

public class PatientMedication
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int CatalogItemId { get; set; }
    public CatalogItem CatalogItem { get; set; } = null!;

    public string Dosage { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public DateTime PrescribedAt { get; set; } = DateTime.Now;
}