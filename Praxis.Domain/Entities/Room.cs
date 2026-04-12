namespace Praxis.Domain.Entities;

public class Room
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Beschreibung { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}