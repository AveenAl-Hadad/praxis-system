using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly PraxisDbContext _db;

    public DocumentService(PraxisDbContext db)
    {
        _db = db;
    }

    public async Task<List<PatientDocument>> GetDocumentsByPatientAsync(int patientId)
    {
        return await _db.PatientDocuments
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<PatientDocument?> GetDocumentByIdAsync(int documentId)
    {
        return await _db.PatientDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId);
    }

    public async Task AddDocumentAsync(PatientDocument document)
    {
        if (document.PatientId <= 0)
            throw new InvalidOperationException("Patient fehlt.");

        if (string.IsNullOrWhiteSpace(document.Title))
            document.Title = document.FileName;

        document.CreatedAt = DateTime.Now;
        document.UploadDate = DateTime.Now;

        _db.PatientDocuments.Add(document);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateDocumentAsync(PatientDocument document)
    {
        var existingDoc = await _db.PatientDocuments.FindAsync(document.Id);

        if (existingDoc == null)
            throw new InvalidOperationException("Dokument wurde nicht gefunden.");

        existingDoc.Title = document.Title;
        existingDoc.DocumentType = document.DocumentType;
        existingDoc.FileName = document.FileName;
        existingDoc.FilePath = document.FilePath;
        existingDoc.Description = document.Description;
        existingDoc.PatientId = document.PatientId;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteDocumentAsync(int documentId)
    {
        var doc = await _db.PatientDocuments.FindAsync(documentId);

        if (doc == null)
            return;

        _db.PatientDocuments.Remove(doc);
        await _db.SaveChangesAsync();
    }
}