using Praxis.Domain.Entities;

namespace Praxis.Infrastructure.Services;

public interface IInvoiceService
{
    Task<List<Invoice>> GetAllInvoicesAsync();
    Task<List<Invoice>> GetInvoicesByPatientAsync(int patientId);
    Task<Invoice?> GetInvoiceByIdAsync(int id);
    Task AddInvoiceAsync(Invoice invoice, string usreName);
    Task DeleteInvoiceAsync(int id,string usreName);
}