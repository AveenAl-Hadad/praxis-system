namespace Praxis.Domain.Entities;

public class DoctorLetter
{
    public int Id { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int? PatientId { get; set; }
    public Patient? Patient { get; set; }
}