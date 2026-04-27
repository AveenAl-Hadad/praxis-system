using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class InvoiceNumberService : IInvoiceNumberService
{
    private readonly PraxisDbContext _db;

    public InvoiceNumberService(PraxisDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateNextInvoiceNumberAsync(DateTime invoiceDate)
    {
        var prefix = $"RE-{invoiceDate:yyyy}-";

        var existingNumbers = await _db.Invoices
            .Where(x => x.InvoiceNumber.StartsWith(prefix))
            .Select(x => x.InvoiceNumber)
            .ToListAsync();

        var nextNumber = existingNumbers
            .Select(x =>
            {
                var numberPart = x.Replace(prefix, "");
                return int.TryParse(numberPart, out var number) ? number : 0;
            })
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{prefix}{nextNumber:0000}";
    }
}