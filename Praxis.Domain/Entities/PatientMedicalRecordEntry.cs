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

    public string InvoiceDisplay =>
    Invoice?.InvoiceNumber ?? "Keine";

    public string BillingStatusDisplay =>
        InvoiceId.HasValue ? "Ja" : "Nein";

    public string EntryTypeDisplay => EntryType.ToString();

    public string EntryTypeColor
    {
        get
        {
            return EntryType switch
            {
                Praxis.Domain.Constants.MedicalRecordEntryType.Anamnese => "#DBEAFE",
                Praxis.Domain.Constants.MedicalRecordEntryType.Befund => "#DCFCE7",
                Praxis.Domain.Constants.MedicalRecordEntryType.Diagnose => "#FEF3C7",
                Praxis.Domain.Constants.MedicalRecordEntryType.Therapie => "#EDE9FE",
                Praxis.Domain.Constants.MedicalRecordEntryType.Notiz => "#F3F4F6",
                Praxis.Domain.Constants.MedicalRecordEntryType.Labor => "#CCFBF1",
                Praxis.Domain.Constants.MedicalRecordEntryType.Dokument => "#E0E7FF",
                Praxis.Domain.Constants.MedicalRecordEntryType.Abrechnung => "#FEE2E2",
                _ => "#FFFFFF"
            };
        }
    }
    public string CreatedAtDisplay => CreatedAt.ToString("dd.MM.yyyy HH:mm");

    public string ShortTextDisplay
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Text))
                return string.Empty;

            return Text.Length <= 120
                ? Text
                : Text.Substring(0, 120) + "...";
        }
    }
}