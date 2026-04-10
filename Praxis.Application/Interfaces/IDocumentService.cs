using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces
{ 
public interface IDocumentService
{
    Task<List<PatientDocument>> GetDocumentsByPatientAsync(int patientId);

    Task AddDocumentAsync(PatientDocument document);

    Task DeleteDocumentAsync(int documentId);
    Task UpdateDocumentAsync(PatientDocument document);

}
}