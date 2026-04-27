namespace Praxis.Domain.Entities;

public class PatientDocument
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public string Title { get; set; } = string.Empty;

    public string DocumentType { get; set; } = "Sonstiges";

    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UploadDate { get; set; } = DateTime.Now;
}