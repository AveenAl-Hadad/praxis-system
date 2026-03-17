namespace Praxis.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;

    public string UserName { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;
}