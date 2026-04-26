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
    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<PatientDiagnosis> PatientDiagnoses => Set<PatientDiagnosis>();

    public DbSet<PatientMedication> PatientMedications => Set<PatientMedication>();
    public DbSet<PracticeSettings> PracticeSettings => Set<PracticeSettings>();
    public DbSet<AppointmentMedicalEntry> AppointmentMedicalEntries => Set<AppointmentMedicalEntry>();
    public DbSet<PatientCase> PatientCases => Set<PatientCase>();
    public DbSet<PatientMedicalRecordEntry> PatientMedicalRecordEntries => Set<PatientMedicalRecordEntry>();
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
             .HasOne(x => x.Invoice)
             .WithMany(x => x.Items)
             .HasForeignKey(x => x.InvoiceId)
             .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InvoiceItem>()
            .Property(x => x.UnitPrice)
            .HasPrecision(10, 2);

        modelBuilder.Entity<InvoiceItem>()
            .Property(x => x.TotalPrice)
            .HasPrecision(10, 2);

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
        modelBuilder.Entity<CatalogItem>()
            .Property(x => x.Category)
            .HasMaxLength(100);

        modelBuilder.Entity<CatalogItem>()
            .Property(x => x.Code)
            .HasMaxLength(50);

        modelBuilder.Entity<CatalogItem>()
            .Property(x => x.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<CatalogItem>()
            .Property(x => x.Description)
            .HasMaxLength(500);

        modelBuilder.Entity<CatalogItem>()
            .Property(x => x.Price)
            .HasPrecision(10,2);

        modelBuilder.Entity<CatalogItem>()
            .HasIndex(x => new { x.Category, x.Code })
            .IsUnique();

        modelBuilder.Entity<PatientDiagnosis>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Diagnoses)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PatientDiagnosis>()
            .HasOne(x => x.CatalogItem)
            .WithMany()
            .HasForeignKey(x => x.CatalogItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientDiagnosis>()
            .Property(x => x.Notes)
            .HasMaxLength(500);

        modelBuilder.Entity<PatientDiagnosis>()
            .HasIndex(x => new { x.PatientId, x.CatalogItemId });
        modelBuilder.Entity<PatientMedication>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Medications)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PatientMedication>()
            .HasOne(x => x.CatalogItem)
            .WithMany()
            .HasForeignKey(x => x.CatalogItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientMedication>()
            .Property(x => x.Dosage)
            .HasMaxLength(100);

        modelBuilder.Entity<PatientMedication>()
            .Property(x => x.Notes)
            .HasMaxLength(500);
        modelBuilder.Entity<PracticeSettings>()
            .Property(x => x.PracticeName)
            .HasMaxLength(200);

        modelBuilder.Entity<PracticeSettings>()
            .Property(x => x.DoctorName)
            .HasMaxLength(200);
        modelBuilder.Entity<AppointmentMedicalEntry>()
            .HasOne(x => x.Appointment)
            .WithMany(x => x.MedicalEntries)
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppointmentMedicalEntry>()
            .HasOne(x => x.DiagnosisCatalogItem)
            .WithMany()
            .HasForeignKey(x => x.DiagnosisCatalogItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppointmentMedicalEntry>()
            .HasOne(x => x.ServiceCatalogItem)
            .WithMany()
            .HasForeignKey(x => x.ServiceCatalogItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppointmentMedicalEntry>()
            .Property(x => x.Notes)
            .HasMaxLength(500);
        modelBuilder.Entity<PatientCase>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Cases)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PatientCase>()
            .Property(x => x.CaseNumber)
            .HasMaxLength(50);

        modelBuilder.Entity<PatientCase>()
            .Property(x => x.InsuranceType)
            .HasMaxLength(30);

        modelBuilder.Entity<PatientCase>()
            .Property(x => x.InsuranceName)
            .HasMaxLength(150);

        modelBuilder.Entity<PatientCase>()
            .Property(x => x.Quarter)
            .HasMaxLength(20);

        modelBuilder.Entity<PatientCase>()
            .Property(x => x.Notes)
            .HasMaxLength(500);

        modelBuilder.Entity<PatientCase>()
            .HasIndex(x => new { x.PatientId, x.Quarter, x.InsuranceType });

        modelBuilder.Entity<PatientMedicalRecordEntry>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.MedicalRecordEntries)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PatientMedicalRecordEntry>()
            .HasOne(x => x.Appointment)
            .WithMany(x => x.MedicalRecordEntries)
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PatientMedicalRecordEntry>()
            .HasOne(x => x.CatalogItem)
            .WithMany()
            .HasForeignKey(x => x.CatalogItemId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PatientMedicalRecordEntry>()
            .HasOne(x => x.LaborRecord)
            .WithMany()
            .HasForeignKey(x => x.LaborRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PatientMedicalRecordEntry>()
            .HasOne(x => x.PatientDocument)
            .WithMany()
            .HasForeignKey(x => x.PatientDocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PatientMedicalRecordEntry>()
            .HasOne(x => x.Invoice)
            .WithMany()
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PatientMedicalRecordEntry>()
            .Property(x => x.Title)
            .HasMaxLength(200);

        modelBuilder.Entity<PatientMedicalRecordEntry>()
            .Property(x => x.Text)
            .HasMaxLength(4000);

        modelBuilder.Entity<PatientMedicalRecordEntry>()
            .Property(x => x.IcdCode)
            .HasMaxLength(30);

        modelBuilder.Entity<PatientMedicalRecordEntry>()
            .Property(x => x.IcdText)
            .HasMaxLength(300);

        modelBuilder.Entity<PatientMedicalRecordEntry>()
            .Property(x => x.CreatedBy)
            .HasMaxLength(100);

        modelBuilder.Entity<PatientMedicalRecordEntry>()
            .HasIndex(x => new { x.PatientId, x.EntryType, x.CreatedAt });
    }
}