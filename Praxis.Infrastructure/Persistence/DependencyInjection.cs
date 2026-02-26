using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string dbPath)
    {
        services.AddDbContext<PraxisDbContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        return services;
    }
}