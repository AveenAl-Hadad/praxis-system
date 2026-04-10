using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;

namespace Praxis.Infrastructure.Persistence;

public class PraxisDbContext : DbContext
{
    public PraxisDbContext(DbContextOptions<PraxisDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Abrechnungsbeleg> Abrechnungsbelegs => Set<Abrechnungsbeleg>();
    public DbSet<LaborRecord> LaborRecords => Set<LaborRecord>();
    public DbSet<DashboardTask> DashboardTasks => Set<DashboardTask>();
    public DbSet<PracticeNotice> PracticeNotices => Set<PracticeNotice>();

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

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Patient)
            .WithMany(p => p.Invoices)
            .HasForeignKey(i => i.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InvoiceItem>()
            .HasOne(ii => ii.Invoice)
            .WithMany(i => i.Items)
            .HasForeignKey(ii => ii.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Invoice>()
            .Property(i => i.TotalAmount)
            .HasColumnType("TEXT");

        modelBuilder.Entity<InvoiceItem>()
            .Property(i => i.UnitPrice)
            .HasColumnType("TEXT");

        modelBuilder.Entity<Prescription>()
            .HasOne(p => p.Patient)
            .WithMany(x => x.Prescriptions)
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PatientDocument>()
            .HasOne(d => d.Patient)
            .WithMany(p => p.Documents)
            .HasForeignKey(d => d.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DashboardTask>()
            .HasOne(t => t.Patient)
            .WithMany()
            .HasForeignKey(t => t.PatientId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DashboardTask>()
            .Property(t => t.Title)
            .HasMaxLength(200);

        modelBuilder.Entity<PracticeNotice>()
            .Property(n => n.Title)
            .HasMaxLength(200);
    }
}