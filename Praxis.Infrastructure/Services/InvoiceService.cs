using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;
public class InvoiceService : IInvoiceService
{
    // DbContext für den Zugriff auf die Datenbank
    private readonly PraxisDbContext _db;

    // Audit-Service zum Protokollieren von Aktionen (z.B. Create/Delete)
    private readonly IAuditService _auditService;

    // Konstruktor (Dependency Injection)
    public InvoiceService(PraxisDbContext db, IAuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    // Alle Rechnungen laden (inkl. Patient + Positionen)
    public async Task<List<Invoice>> GetAllInvoicesAsync()
    {
        return await _db.Invoices
            .Include(i => i.Patient)   // Patient-Daten mitladen
            .Include(i => i.Items)     // Rechnungspositionen mitladen
            .OrderByDescending(i => i.InvoiceDate) // Neueste zuerst
            .ToListAsync();
    }

    // Alle Rechnungen für einen bestimmten Patienten
    public async Task<List<Invoice>> GetInvoicesByPatientAsync(int patientId)
    {
        return await _db.Invoices
            .Include(i => i.Patient)
            .Include(i => i.Items)
            .Where(i => i.PatientId == patientId) // Filter nach Patient
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    // Eine einzelne Rechnung anhand der ID laden
    public async Task<Invoice?> GetInvoiceByIdAsync(int id)
    {
        return await _db.Invoices
            .Include(i => i.Patient)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id); // null wenn nicht gefunden
    }

    // Neue Rechnung erstellen
    public async Task AddInvoiceAsync(Invoice invoice, string userName)
    {
        // Gesamtbetrag berechnen aus allen Positionen
        invoice.TotalAmount = invoice.Items.Sum(x => x.TotalPrice);

        // Falls keine Rechnungsnummer vorhanden → automatisch generieren
        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            invoice.InvoiceNumber = $"RE-{DateTime.Now:yyyyMMddHHmmss}";
        }

        // Rechnung zur DB hinzufügen
        _db.Invoices.Add(invoice);

        // Änderungen speichern
        await _db.SaveChangesAsync();

        // Aktion im Audit-Log speichern
        await _auditService.LogAsync(userName,
                                    "CREATE",
                                    "Invoice",
                                    $"Rechnung {invoice.InvoiceNumber} erstellt");
    }

    // Rechnung löschen
    public async Task DeleteInvoiceAsync(int id, string userName)
    {
        // Rechnung anhand ID suchen
        var invoice = await _db.Invoices.FindAsync(id);

        // Nur löschen, wenn gefunden
        if (invoice != null)
        {
            // Rechnungsnummer merken für das Log
            var invoiceNumber = invoice.InvoiceNumber;

            // Rechnung entfernen
            _db.Invoices.Remove(invoice);

            // Änderungen speichern
            await _db.SaveChangesAsync();

            // Löschvorgang im Audit-Log speichern
            await _auditService.LogAsync(
                userName,
                "DELETE",
                "Invoice",
                $"Rechnung {invoiceNumber} gelöscht");
        }
    }
}