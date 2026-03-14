using Praxis.Domain.Entities;

namespace Praxis.Infrastructure.Services;

public interface IInvoicePdfService
{
    void ExportInvoiceToPdf(Invoice invoice, string filePath);
}