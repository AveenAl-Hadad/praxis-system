using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface IRoomService
{
    Task<List<Room>> GetAllAsync();
    Task<List<Room>> GetActiveAsync();
    Task<Room?> GetByIdAsync(int id);
    Task AddAsync(Room room);
    Task UpdateAsync(Room room);
    Task DeleteAsync(int id);
}