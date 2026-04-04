using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;

namespace Praxis.Domain.Entities;

public class Patient
{
    public int Id { get; set; }
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public string FullName => $"{Vorname} {Nachname}";
    public DateTime Geburtsdatum { get; set; }
    public string Telefonnummer { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    public string Adresse { get; set; } = string.Empty;
    public string PLZ { get; set; } = string.Empty;
    public string Ort { get; set; } = string.Empty;
    public string Versicherung { get; set; } = string.Empty;
    public string Geschlecht { get; set; } = string.Empty;
    public string Versichertennummer { get; set; } = string.Empty;

    [NotMapped]
    public int Alter
    {
        get
        {
            var today = DateTime.Today;
            var age = today.Year - Geburtsdatum.Year;

            if (Geburtsdatum.Date > today.AddYears(-age))
                age--;

            return age;
        }
    }
    public bool IsActive { get; set; } = true;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<PatientDocument> Documents { get; set; } = new List<PatientDocument>();
}