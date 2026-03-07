using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;

namespace Praxis.Infrastructure.Persistence;

public class PraxisDbContext : DbContext
{
    public PraxisDbContext(DbContextOptions<PraxisDbContext> options) : base(options) { }

    public DbSet<Patient> Patients { get; set; }

    public DbSet<Appointment> Appointments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.Email)
            .IsUnique();

        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.Telefonnummer)
            .IsUnique();

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}