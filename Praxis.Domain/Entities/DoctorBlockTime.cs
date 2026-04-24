namespace Praxis.Domain.Entities;

public class DoctorBlockTime
{
    public int Id { get; set; }

    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    /// <summary>
    /// Beginn der Sperrzeit.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Ende der Sperrzeit.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Urlaub | Fortbildung | OP | Abwesend | Manuell gesperrt
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    public bool IsAllDay { get; set; } = false;

    public bool IsActive { get; set; } = true;
}