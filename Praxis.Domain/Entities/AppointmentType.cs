namespace Praxis.Domain.Entities;

public class AppointmentType
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Standarddauer dieser Terminart.
    /// </summary>
    public int DurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Darf diese Terminart online gebucht werden?
    /// </summary>
    public bool AllowOnlineBooking { get; set; } = true;

    /// <summary>
    /// Frühestens so viele Stunden vor Termin online buchbar.
    /// </summary>
    public int MinLeadHours { get; set; } = 2;

    /// <summary>
    /// Höchstens so viele Tage im Voraus online buchbar.
    /// </summary>
    public int MaxAdvanceDays { get; set; } = 90;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<DoctorAppointmentType> DoctorAppointmentTypes { get; set; } = new List<DoctorAppointmentType>();
}