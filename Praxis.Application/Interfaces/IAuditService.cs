using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces
{ 

public interface IAuditService
{
    Task LogAsync(string userName, string action, string entityType, string details);
    Task<List<AuditLog>> GetLogsAsync();
}
}