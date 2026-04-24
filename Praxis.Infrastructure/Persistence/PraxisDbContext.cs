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
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<AppointmentType> AppointmentTypes => Set<AppointmentType>();

    public DbSet<DoctorAppointmentType> DoctorAppointmentTypes => Set<DoctorAppointmentType>();
    public DbSet<DoctorBlockTime> DoctorBlockTimes => Set<DoctorBlockTime>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.Email)
            .IsUnique()
            .HasFilter("Email IS NOT NULL AND TRIM(Email) <> ''");

        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.Telefonnummer)
            .IsUnique()
            .HasFilter("Telefonnummer IS NOT NULL AND TRIM(Telefonnummer) <> ''");

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
        modelBuilder.Entity<Room>()
            .HasIndex(r => r.Name)
            .IsUnique();

        modelBuilder.Entity<Room>()
            .Property(r => r.Name)
            .HasMaxLength(100);

        modelBuilder.Entity<Room>()
            .Property(r => r.Beschreibung)
            .HasMaxLength(300);

        modelBuilder.Entity<Doctor>()
    .Property(d => d.Title)
    .HasMaxLength(50);

        modelBuilder.Entity<Doctor>()
            .Property(d => d.FirstName)
            .HasMaxLength(100);

        modelBuilder.Entity<Doctor>()
            .Property(d => d.LastName)
            .HasMaxLength(100);

        modelBuilder.Entity<Doctor>()
            .Property(d => d.Specialty)
            .HasMaxLength(150);

        modelBuilder.Entity<Doctor>()
            .Property(d => d.DefaultRoomName)
            .HasMaxLength(100);

        modelBuilder.Entity<AppointmentType>()
            .Property(t => t.Name)
            .HasMaxLength(100);

        modelBuilder.Entity<AppointmentType>()
            .Property(t => t.Description)
            .HasMaxLength(300);

        modelBuilder.Entity<DoctorSchedule>()
            .HasOne(s => s.Doctor)
            .WithMany(d => d.Schedules)
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Doctor)
            .WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.AppointmentType)
            .WithMany(t => t.Appointments)
            .HasForeignKey(a => a.AppointmentTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DoctorSchedule>()
            .HasIndex(s => new { s.DoctorId, s.DayOfWeek, s.StartTime, s.EndTime });

        modelBuilder.Entity<DoctorAppointmentType>()
    .HasKey(x => new { x.DoctorId, x.AppointmentTypeId });

        modelBuilder.Entity<DoctorAppointmentType>()
            .HasOne(x => x.Doctor)
            .WithMany(d => d.DoctorAppointmentTypes)
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DoctorAppointmentType>()
            .HasOne(x => x.AppointmentType)
            .WithMany(t => t.DoctorAppointmentTypes)
            .HasForeignKey(x => x.AppointmentTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DoctorBlockTime>()
            .HasOne(x => x.Doctor)
            .WithMany(d => d.BlockTimes)
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DoctorBlockTime>()
            .Property(x => x.Reason)
            .HasMaxLength(150);

        modelBuilder.Entity<DoctorBlockTime>()
            .HasIndex(x => new { x.DoctorId, x.StartTime, x.EndTime });
    }
}