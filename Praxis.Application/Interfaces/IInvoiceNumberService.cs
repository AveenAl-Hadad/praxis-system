namespace Praxis.Application.Interfaces;

public interface IInvoiceNumberService
{
    Task<string> GenerateNextInvoiceNumberAsync(DateTime invoiceDate);
}