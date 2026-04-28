namespace Praxis.Domain.Entities;

public class PracticeMessage
{
    public int Id { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public string Sender { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;

    public string Priority { get; set; } = "Normal"; // Normal | Wichtig | Dringend
    public bool IsRead { get; set; } = false;

    public int? PatientId { get; set; }
    public Patient? Patient { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}