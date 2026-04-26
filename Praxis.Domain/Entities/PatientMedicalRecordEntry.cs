using Praxis.Domain.Constants;

namespace Praxis.Domain.Entities;

public class PatientMedicalRecordEntry
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public MedicalRecordEntryType EntryType { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public string? IcdCode { get; set; }
    public string? IcdText { get; set; }

    public int? CatalogItemId { get; set; }
    public CatalogItem? CatalogItem { get; set; }

    public int? LaborRecordId { get; set; }
    public LaborRecord? LaborRecord { get; set; }

    public int? PatientDocumentId { get; set; }
    public PatientDocument? PatientDocument { get; set; }

    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
}