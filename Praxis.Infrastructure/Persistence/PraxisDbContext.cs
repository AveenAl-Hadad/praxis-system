using Microsoft.EntityFrameworkCore;

namespace Praxis.Infrastructure.Persistence;

public class PraxisDbContext : DbContext
{
    public PraxisDbContext(DbContextOptions<PraxisDbContext> options) : base(options) { }

    // PS-7: DbSet<Patient> kommt später hier rein
}