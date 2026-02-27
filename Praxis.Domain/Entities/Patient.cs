namespace Praxis.Domain.Entities;

public class Patient
{
    public int Id { get; set; }

    public string Vorname { get; set; } = string.Empty;

    public string Nachname { get; set; } = string.Empty;

    public DateTime Geburtsdatum { get; set; }

    public string Telefonnummer { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}