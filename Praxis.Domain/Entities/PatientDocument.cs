namespace Praxis.Domain.Entities;

public class PatientDocument
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadDate { get; set; } = DateTime.Now;
}