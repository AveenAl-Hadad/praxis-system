namespace Praxis.Domain.Entities;

public class ExternalMessage
{
    public int Id { get; set; }

    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public string Status { get; set; } = "Neu"; // Neu | Bearbeitet | Archiviert
    public bool IsRead { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.Now;

    public int? PatientId { get; set; }
    public Patient? Patient { get; set; }
}