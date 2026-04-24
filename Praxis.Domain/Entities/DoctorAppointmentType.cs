namespace Praxis.Domain.Entities;

public class DoctorAppointmentType
{
    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public int AppointmentTypeId { get; set; }
    public AppointmentType? AppointmentType { get; set; }

    /// <summary>
    /// Optional: Arzt ist für diese Terminart bevorzugt sichtbar.
    /// </summary>
    public bool IsPreferred { get; set; } = false;
}