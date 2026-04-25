using Praxis.Domain.Entities;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;


namespace Praxis.Client.Views;

public partial class PrescriptionPreviewWindow : Window
{
    public PrescriptionPreviewWindow(Patient patient, IEnumerable<PatientMedication> medications)
    {
        InitializeComponent();
        BuildDocument(patient, medications);
    }

    private void BuildDocument(Patient patient, IEnumerable<PatientMedication> medications)
    {
        PrescriptionDocument.Blocks.Clear();

        PrescriptionDocument.Blocks.Add(new Paragraph(new Run("Rezept / Medikamentenverordnung"))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold
        });

        PrescriptionDocument.Blocks.Add(new Paragraph(new Run(
            $"Patient: {patient.Vorname} {patient.Nachname}\n" +
            $"Geburtsdatum: {patient.Geburtsdatum:dd.MM.yyyy}\n" +
            $"Datum: {DateTime.Now:dd.MM.yyyy}"
        )));

        PrescriptionDocument.Blocks.Add(new Paragraph(new Run("Medikamente"))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 20, 0, 8)
        });

        foreach (var medication in medications)
        {
            var text =
                $"{medication.CatalogItem.Name}\n" +
                $"Dosierung: {medication.Dosage}\n" +
                $"Notiz: {medication.Notes}\n";

            PrescriptionDocument.Blocks.Add(new Paragraph(new Run(text))
            {
                Margin = new Thickness(0, 0, 0, 12)
            });
        }

        PrescriptionDocument.Blocks.Add(new Paragraph(new Run(
            "\n\nUnterschrift Arzt: ______________________________"
        )));
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Controls.PrintDialog();

        if (dialog.ShowDialog() == true)
        {
            dialog.PrintDocument(
                ((IDocumentPaginatorSource)PrescriptionDocument).DocumentPaginator,
                "Rezept");
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}