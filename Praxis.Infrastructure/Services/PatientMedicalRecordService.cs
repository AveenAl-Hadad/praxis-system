using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Constants;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class PatientMedicalRecordService : IPatientMedicalRecordService
{
    private readonly PraxisDbContext _context;

    public PatientMedicalRecordService(PraxisDbContext context)
    {
        _context = context;
    }

    public async Task<List<PatientMedicalRecordEntry>> GetByPatientAsync(int patientId)
    {
        return await _context.PatientMedicalRecordEntries
            .AsNoTracking()
            .Include(x => x.CatalogItem)
            .Include(x => x.LaborRecord)
            .Include(x => x.PatientDocument)
            .Include(x => x.Invoice)
            .Where(x => x.PatientId == patientId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<PatientMedicalRecordEntry>> GetByPatientAndTypeAsync( int patientId, MedicalRecordEntryType entryType)
    {
        return await _context.PatientMedicalRecordEntries
            .AsNoTracking()
            .Where(x =>
                x.PatientId == patientId &&
                x.EntryType == entryType &&
                !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<PatientMedicalRecordEntry> AddAsync(PatientMedicalRecordEntry entry)
    {
        if (entry.PatientId <= 0)
            throw new InvalidOperationException("Patient fehlt.");

        if (string.IsNullOrWhiteSpace(entry.Title))
            entry.Title = entry.EntryType.ToString();

        if (string.IsNullOrWhiteSpace(entry.Text)
            && entry.EntryType != MedicalRecordEntryType.Dokument
            && entry.EntryType != MedicalRecordEntryType.Labor
            && entry.EntryType != MedicalRecordEntryType.Abrechnung)
        {
            throw new InvalidOperationException("Der Karteikarten-Text darf nicht leer sein.");
        }

        entry.Title = entry.Title.Trim();
        entry.Text = entry.Text?.Trim() ?? string.Empty;
        entry.CreatedAt = DateTime.Now;
        entry.IsDeleted = false;

        _context.PatientMedicalRecordEntries.Add(entry);
        await _context.SaveChangesAsync();

        return entry;
    }

    public async Task UpdateAsync(PatientMedicalRecordEntry entry)
    {
        var existing = await _context.PatientMedicalRecordEntries.FindAsync(entry.Id);

        if (existing == null || existing.IsDeleted)
            throw new InvalidOperationException("Karteikarten-Eintrag wurde nicht gefunden.");

        existing.EntryType = entry.EntryType;
        existing.Title = entry.Title.Trim();
        existing.Text = entry.Text?.Trim() ?? string.Empty;
        existing.IcdCode = entry.IcdCode;
        existing.IcdText = entry.IcdText;
        existing.CatalogItemId = entry.CatalogItemId;
        existing.LaborRecordId = entry.LaborRecordId;
        existing.PatientDocumentId = entry.PatientDocumentId;
        existing.InvoiceId = entry.InvoiceId;
        existing.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await _context.PatientMedicalRecordEntries.FindAsync(id);

        if (existing == null)
            return;

        existing.IsDeleted = true;
        existing.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
    }
    public async Task MarkEntriesAsInvoicedAsync(List<int> entryIds, int invoiceId)
    {
        var entries = await _context.PatientMedicalRecordEntries
            .Where(x => entryIds.Contains(x.Id))
            .ToListAsync();

        foreach (var entry in entries)
        {
            entry.InvoiceId = invoiceId;
            entry.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }
}