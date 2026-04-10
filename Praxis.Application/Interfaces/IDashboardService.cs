using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces
{ 

public interface IDashboardService
{
    Task<DashboardStats> GetStatsAsync();
}
}