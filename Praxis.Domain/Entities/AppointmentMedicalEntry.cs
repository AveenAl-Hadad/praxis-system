namespace Praxis.Domain.Entities;

public class AppointmentMedicalEntry
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;

    public int? DiagnosisCatalogItemId { get; set; }
    public CatalogItem? DiagnosisCatalogItem { get; set; }

    public int? ServiceCatalogItemId { get; set; }
    public CatalogItem? ServiceCatalogItem { get; set; }

    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}