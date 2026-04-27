using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class BillingGenerationService : IBillingGenerationService
{
    private readonly PraxisDbContext _context;
    private readonly IInvoiceNumberService _invoiceNumberService;


    public BillingGenerationService(
        PraxisDbContext context,
        IInvoiceNumberService invoiceNumberService)
    {
        _context = context;
        _invoiceNumberService = invoiceNumberService;
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

        var invoiceDate = DateTime.Now;

        var invoice = new Invoice
        {
            PatientId = appointment.PatientId,
            InvoiceDate = invoiceDate,
            InvoiceNumber = await _invoiceNumberService.GenerateNextInvoiceNumberAsync(invoiceDate)
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

    public async Task<Invoice> CreateInvoiceFromMedicalRecordEntriesAsync(
        int patientId,
        List<int> medicalRecordEntryIds)
    {
        var patient = await _context.Patients.FindAsync(patientId);

        if (patient == null)
            throw new InvalidOperationException("Patient wurde nicht gefunden.");

        var entries = await _context.PatientMedicalRecordEntries
            .Include(x => x.CatalogItem)
            .Where(x =>
                x.PatientId == patientId &&
                medicalRecordEntryIds.Contains(x.Id) &&
                x.CatalogItemId != null &&
                !x.IsDeleted)
            .ToListAsync();

        if (entries.Count == 0)
            throw new InvalidOperationException("Keine abrechenbaren Karteikarten-Leistungen gefunden.");

        var invoiceDate = DateTime.Now;

        var invoice = new Invoice
        {
            PatientId = patientId,
            InvoiceDate = invoiceDate,
            InvoiceNumber = await _invoiceNumberService.GenerateNextInvoiceNumberAsync(invoiceDate)
        };

        foreach (var entry in entries)
        {
            var item = entry.CatalogItem!;

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