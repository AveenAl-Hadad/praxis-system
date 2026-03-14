using Praxis.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Praxis.Infrastructure.Services;

public class InvoicePdfService : IInvoicePdfService
{
    public void ExportInvoiceToPdf(Invoice invoice, string filePath)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .Text($"Rechnung {invoice.InvoiceNumber}")
                    .FontSize(20)
                    .Bold();

                page.Content().Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Text($"Datum: {invoice.InvoiceDate:d}");
                    col.Item().Text($"Patient: {invoice.Patient?.FullName}");
                    col.Item().Text($"E-Mail: {invoice.Patient?.Email}");
                    col.Item().Text($"Telefon: {invoice.Patient?.Telefonnummer}");

                    col.Item().LineHorizontal(1);

                    foreach (var item in invoice.Items)
                    {
                        col.Item().Text(
                            $"{item.Description} | Menge: {item.Quantity} | Einzelpreis: {item.UnitPrice:N2} € | Gesamt: {item.TotalPrice:N2} €");
                    }

                    col.Item().LineHorizontal(1);
                    col.Item().Text($"Gesamtbetrag: {invoice.TotalAmount:N2} €").Bold();
                });
            });
        }).GeneratePdf(filePath);
    }
}