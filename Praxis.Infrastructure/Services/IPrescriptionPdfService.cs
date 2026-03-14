using Praxis.Domain.Entities;

namespace Praxis.Infrastructure.Services;

public interface IPrescriptionPdfService
{
    void ExportPrescriptionToPdf(Prescription prescription, string filePath);
}