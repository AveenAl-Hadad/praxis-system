namespace Praxis.Domain.Entities;

public class Appointment
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public DateTime StartTime { get; set; }

    public int DurationMinutes { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = "Geplant";

    // Wartezimmer
    public string RoomName { get; set; } = string.Empty;
    public int? QueueNumber { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public string InternalNote { get; set; } = string.Empty;

    /// <summary>
    /// Geplant | Angemeldet | ImWartezimmer | ImBehandlungsraum | Erledigt | Abgesagt
    /// </summary>
    public string TreatmentState { get; set; } = "Geplant";

    public DateTime EndTime => StartTime.AddMinutes(DurationMinutes);
}