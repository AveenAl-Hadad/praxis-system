using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface ICatalogService
{
    Task<List<CatalogItem>> GetAllAsync();
    Task<List<CatalogItem>> GetByCategoryAsync(string category);
    Task AddAsync(CatalogItem item);
    Task UpdateAsync(CatalogItem item);
    Task DeleteAsync(int id);
}