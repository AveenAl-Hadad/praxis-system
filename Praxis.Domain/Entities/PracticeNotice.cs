namespace Praxis.Domain.Entities;

public class PracticeNotice
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Info | Wichtig | Warnung
    /// </summary>
    public string Category { get; set; } = "Info";

    public bool IsPinned { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? VisibleUntil { get; set; }
}