using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly PraxisDbContext _db;
    private readonly IAuditService _auditService;

    public InvoiceService(PraxisDbContext db, IAuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    public async Task<List<Invoice>> GetAllInvoicesAsync()
    {
        return await _db.Invoices
            .Include(i => i.Patient)
            .Include(i => i.Items)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<List<Invoice>> GetInvoicesByPatientAsync(int patientId)
    {
        return await _db.Invoices
            .Include(i => i.Patient)
            .Include(i => i.Items)
            .Where(i => i.PatientId == patientId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(int id)
    {
        return await _db.Invoices
            .Include(i => i.Patient)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task AddInvoiceAsync(Invoice invoice, string userName)
    {
        invoice.TotalAmount = invoice.Items.Sum(x => x.TotalPrice);

        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            invoice.InvoiceNumber = $"RE-{DateTime.Now:yyyyMMddHHmmss}";
        }

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        await _auditService.LogAsync(userName,
                                    "CREATE",
                                    "Invoice",
                                    $"Rechnung {invoice.InvoiceNumber} erstellt");
    }
    public async Task DeleteInvoiceAsync(int id, string userName)
    {
        var invoice = await _db.Invoices.FindAsync(id);

        if (invoice != null)
        {
            var invoiceNumber = invoice.InvoiceNumber;

            _db.Invoices.Remove(invoice);
            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                userName,
                "DELETE",
                "Invoice",
                $"Rechnung {invoiceNumber} gelöscht");
        }
    }
}