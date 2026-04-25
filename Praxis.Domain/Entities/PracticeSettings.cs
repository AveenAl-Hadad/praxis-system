namespace Praxis.Domain.Entities;

public class PracticeSettings
{
    public int Id { get; set; }

    public string PracticeName { get; set; } = "Praxissoftware";
    public string DoctorName { get; set; } = "Dr. med. Musterarzt";
    public string Street { get; set; } = "";
    public string ZipCity { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
}