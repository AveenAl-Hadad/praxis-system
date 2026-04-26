using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class BillingGenerationService : IBillingGenerationService
{
    private readonly PraxisDbContext _context;

    public BillingGenerationService(PraxisDbContext context)
    {
        _context = context;
    }

    public async Task<Invoice> CreateInvoiceFromAppointmentAsync(int appointmentId)
    {
        var appointment = await _context.Appointments
            .Include(x => x.Patient)
            .FirstOrDefaultAsync(x => x.Id == appointmentId);

        if (appointment == null)
            throw new InvalidOperationException("Termin wurde nicht gefunden.");

        var services = await _context.AppointmentMedicalEntries
            .Include(x => x.ServiceCatalogItem)
            .Where(x => x.AppointmentId == appointmentId && x.ServiceCatalogItemId != null)
            .ToListAsync();

        if (services.Count == 0)
            throw new InvalidOperationException("Dieser Termin enthält keine Leistungen.");

        var invoice = new Invoice
        {
            PatientId = appointment.PatientId,
            InvoiceDate = DateTime.Now,
            InvoiceNumber = $"RE-{DateTime.Now:yyyyMMdd-HHmmss}"
        };

        foreach (var service in services)
        {
            var item = service.ServiceCatalogItem!;

            invoice.Items.Add(new InvoiceItem
            {
                Code = item.Code,
                Description = item.Name,
                Quantity = 1,
                UnitPrice = item.Price,
                TotalPrice = item.Price
            });
        }

        invoice.TotalAmount = invoice.Items.Sum(x => x.TotalPrice);

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        return invoice;
    }
}