namespace Praxis.Domain.Entities;

public class Doctor
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public string Specialty { get; set; } = string.Empty;

    /// <summary>
    /// Standardraum für Termine dieses Behandlers.
    /// </summary>
    public string DefaultRoomName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Darf dieser Behandler online gebucht werden?
    /// </summary>
    public bool AllowOnlineBooking { get; set; } = true;

    public string FullName =>
        string.IsNullOrWhiteSpace(Title)
            ? $"{FirstName} {LastName}".Trim()
            : $"{Title} {FirstName} {LastName}".Trim();

    public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<DoctorAppointmentType> DoctorAppointmentTypes { get; set; } = new List<DoctorAppointmentType>();
    public ICollection<DoctorBlockTime> BlockTimes { get; set; } = new List<DoctorBlockTime>();
}