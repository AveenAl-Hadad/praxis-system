using Praxis.Domain.Entities;

namespace Praxis.Infrastructure.Services;

public interface IDashboardService
{
    Task<DashboardStats> GetStatsAsync();
}