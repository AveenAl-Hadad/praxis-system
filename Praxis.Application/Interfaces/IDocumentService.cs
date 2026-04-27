using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface IDocumentService
{
    Task<List<PatientDocument>> GetDocumentsByPatientAsync(int patientId);

    Task<PatientDocument?> GetDocumentByIdAsync(int documentId);

    Task AddDocumentAsync(PatientDocument document);

    Task UpdateDocumentAsync(PatientDocument document);

    Task DeleteDocumentAsync(int documentId);
}