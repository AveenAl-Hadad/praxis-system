using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Application.Interfaces;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;
/// <summary>
/// Service zur Verwaltung von Patientendokumenten.
/// Ermöglicht Laden, Hinzufügen und Löschen von Dokumenten.
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly PraxisDbContext _db;

    /// <summary>
    /// Konstruktor mit Dependency Injection für den DbContext.
    /// </summary>
    public DocumentService(PraxisDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Gibt alle Dokumente eines Patienten zurück (neueste zuerst).
    /// </summary>
    public async Task<List<PatientDocument>> GetDocumentsByPatientAsync(int patientId)
    {
        return await _db.PatientDocuments
            .Where(d => d.PatientId == patientId) // Filter nach Patient
            .OrderByDescending(d => d.UploadDate) // neueste zuerst
            .ToListAsync();
    }

    /// <summary>
    /// Fügt ein neues Dokument hinzu.
    /// </summary>
    public async Task AddDocumentAsync(PatientDocument document)
    {
        _db.PatientDocuments.Add(document);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Löscht ein Dokument anhand der ID.
    /// </summary>
    public async Task DeleteDocumentAsync(int documentId)
    {
        var doc = await _db.PatientDocuments.FindAsync(documentId);

        if (doc != null)
        {
            _db.PatientDocuments.Remove(doc);
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Aktualisiert ein vorhandenes Dokument.
    /// </summary>
    public async Task UpdateDocumentAsync(PatientDocument document)
    {
        var existingDoc = await _db.PatientDocuments.FindAsync(document.Id);

        if (existingDoc != null)
        {
            existingDoc.FileName = document.FileName;
            existingDoc.FilePath = document.FilePath;
            existingDoc.PatientId = document.PatientId;

            await _db.SaveChangesAsync();
        }
    }

}