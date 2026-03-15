using Microsoft.EntityFrameworkCore;
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
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync();
    }

    public async Task AddDocumentAsync(PatientDocument document)
    {
        _db.PatientDocuments.Add(document);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteDocumentAsync(int documentId)
    {
        var doc = await _db.PatientDocuments.FindAsync(documentId);

        if (doc != null)
        {
            _db.PatientDocuments.Remove(doc);
            await _db.SaveChangesAsync();
        }
    }
}