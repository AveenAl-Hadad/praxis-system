using Praxis.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Praxis.Infrastructure.Services;

public class PrescriptionPdfService : IPrescriptionPdfService
{
    public void ExportPrescriptionToPdf(Prescription prescription, string filePath)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .Text("Rezept")
                    .FontSize(22)
                    .Bold();

                page.Content().Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Text($"Rezeptnummer: {prescription.PrescriptionNumber}");
                    col.Item().Text($"Datum: {prescription.IssueDate:d}");
                    col.Item().Text($"Patient: {prescription.Patient?.FullName}");
                    col.Item().Text($"Arzt: {prescription.DoctorName}");

                    col.Item().LineHorizontal(1);

                    col.Item().Text($"Medikament: {prescription.MedicationName}");
                    col.Item().Text($"Dosierung: {prescription.Dosage}");
                    col.Item().Text($"Anweisung: {prescription.Instructions}");

                    col.Item().LineHorizontal(1);
                    col.Item().Text("Unterschrift Arzt: ______________________");
                });
            });
        }).GeneratePdf(filePath);
    }
}