using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces
{ 
public interface IInvoicePdfService
{
    void ExportInvoiceToPdf(Invoice invoice, string filePath);
}
}