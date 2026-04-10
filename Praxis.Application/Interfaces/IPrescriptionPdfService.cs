using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces
{ 
public interface IPrescriptionPdfService
{
    void ExportPrescriptionToPdf(Prescription prescription, string filePath);
}
}