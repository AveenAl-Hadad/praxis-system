using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Praxis.Infrastructure.Services;

/// <summary>
/// Service zum Erstellen professioneller PDF-Rechnungen mit QuestPDF.
/// </summary>
public class InvoicePdfService : IInvoicePdfService
{
    /// <summary>
    /// Exportiert eine Rechnung als PDF-Datei.
    /// </summary>
    public void ExportInvoiceToPdf(Invoice invoice, string filePath)
    {
        // 🔒 Validierung
        if (invoice == null)
            throw new ArgumentNullException(nameof(invoice));

        if (invoice.Items == null || !invoice.Items.Any())
            throw new InvalidOperationException("Rechnung enthält keine Positionen.");

        // Lizenz setzen
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                // 🧾 HEADER
                page.Header().Column(header =>
                {
                    header.Item()
                        .Text($"Rechnung {invoice.InvoiceNumber}")
                        .FontSize(20)
                        .Bold();

                    header.Item()
                        .Text($"Datum: {invoice.InvoiceDate:d}")
                        .FontSize(10);
                });

                // 📄 CONTENT
                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    // 👤 Patientendaten
                    col.Item().Text("Patientendaten").Bold();

                    col.Item().Text($"Name: {invoice.Patient?.FullName}");
                    col.Item().Text($"E-Mail: {invoice.Patient?.Email}");
                    col.Item().Text($"Telefon: {invoice.Patient?.Telefonnummer}");

                    col.Item().LineHorizontal(1);

                    // 📊 TABELLE für Rechnungspositionen
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Beschreibung
                            columns.RelativeColumn(1); // Menge
                            columns.RelativeColumn(2); // Einzelpreis
                            columns.RelativeColumn(2); // Gesamt
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Text("Leistung").Bold();
                            header.Cell().Text("Menge").Bold();
                            header.Cell().Text("Preis").Bold();
                            header.Cell().Text("Gesamt").Bold();
                        });

                        // Daten
                        foreach (var item in invoice.Items)
                        {
                            table.Cell().Text(item.Description);
                            table.Cell().Text(item.Quantity.ToString());
                            table.Cell().Text($"{item.UnitPrice:N2} €");
                            table.Cell().Text($"{item.TotalPrice:N2} €");
                        }
                    });

                    col.Item().LineHorizontal(1);

                    // 💰 Gesamtbetrag
                    col.Item()
                        .AlignRight()
                        .Text($"Gesamtbetrag: {invoice.TotalAmount:N2} €")
                        .FontSize(14)
                        .Bold();
                });

                // 📌 FOOTER
                page.Footer()
                    .AlignCenter()
                    .Text("Vielen Dank für Ihren Besuch!")
                    .FontSize(10);
            });
        })
        .GeneratePdf(filePath);
    }
}