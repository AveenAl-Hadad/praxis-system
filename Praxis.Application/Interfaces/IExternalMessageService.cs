using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface IExternalMessageService
{
    Task<List<ExternalMessage>> GetAllAsync();
    Task AddAsync(ExternalMessage message);
    Task MarkAsReadAsync(int id);
    Task MarkAsProcessedAsync(int id);
    Task DeleteAsync(int id);
}