namespace Praxis.Domain.Entities;

public class DashboardTask
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Offen | InBearbeitung | Erledigt
    /// </summary>
    public string Status { get; set; } = "Offen";

    /// <summary>
    /// Niedrig | Normal | Hoch
    /// </summary>
    public string Priority { get; set; } = "Normal";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? DueDate { get; set; }

    public int? PatientId { get; set; }
    public Patient? Patient { get; set; }

    public string AssignedTo { get; set; } = string.Empty;
}