using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Praxis.Infrastructure.Services;

public class InvoicePdfService : IInvoicePdfService
{
    public void ExportInvoiceToPdf(Invoice invoice, string filePath)
    {
        if (invoice.Items == null || !invoice.Items.Any())
            throw new InvalidOperationException("Rechnung enthält keine Positionen.");

        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(35);

                page.Header().Column(col =>
                {
                    col.Item().Text($"Rechnung {invoice.InvoiceNumber}")
                        .FontSize(22)
                        .Bold();

                    col.Item().Text($"Datum: {invoice.InvoiceDate:dd.MM.yyyy}");
                });

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text("Patient").Bold();
                    col.Item().Text(invoice.Patient?.FullName ?? "");
                    col.Item().Text(invoice.Patient?.Adresse ?? "");
                    col.Item().Text($"{invoice.Patient?.PLZ} {invoice.Patient?.Ort}");

                    col.Item().LineHorizontal(1);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Code").Bold();
                            header.Cell().Text("Leistung").Bold();
                            header.Cell().Text("Menge").Bold();
                            header.Cell().Text("Einzel").Bold();
                            header.Cell().Text("Gesamt").Bold();
                        });

                        foreach (var item in invoice.Items)
                        {
                            table.Cell().Text(item.Code);
                            table.Cell().Text(item.Description);
                            table.Cell().Text(item.Quantity.ToString("N2"));
                            table.Cell().Text($"{item.UnitPrice:N2} €");
                            table.Cell().Text($"{item.TotalPrice:N2} €");
                        }
                    });

                    col.Item().LineHorizontal(1);

                    col.Item()
                        .AlignRight()
                        .Text($"Gesamtbetrag: {invoice.TotalAmount:N2} €")
                        .FontSize(16)
                        .Bold();
                });

                page.Footer()
                    .AlignCenter()
                    .Text("Vielen Dank für Ihren Besuch.");
            });
        }).GeneratePdf(filePath);
    }
}