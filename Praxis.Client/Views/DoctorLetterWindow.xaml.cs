using Praxis.Domain.Entities;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using PrintDialog = System.Windows.Controls.PrintDialog;

namespace Praxis.Client.Views;

public partial class DoctorLetterWindow : Window
{
    public DoctorLetterWindow(
        Patient patient,
        IEnumerable<PatientDiagnosis> diagnoses,
        IEnumerable<PatientMedication> medications,
        PracticeSettings? settings)
    {
        InitializeComponent();
        BuildDocument(patient, diagnoses, medications, settings);
    }

    private void BuildDocument(
        Patient patient,
        IEnumerable<PatientDiagnosis> diagnoses,
        IEnumerable<PatientMedication> medications,
        PracticeSettings? settings)
    {
        LetterDocument.Blocks.Clear();

        if (settings != null)
        {
            LetterDocument.Blocks.Add(new Paragraph(new Run(
                $"{settings.PracticeName}\n" +
                $"{settings.DoctorName}\n" +
                $"{settings.Street}\n" +
                $"{settings.ZipCity}\n" +
                $"Tel: {settings.Phone}\n" +
                $"E-Mail: {settings.Email}"
            ))
            {
                FontSize = 11,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 25)
            });
        }

        LetterDocument.Blocks.Add(new Paragraph(new Run("Arztbrief"))
        {
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 20)
        });

        LetterDocument.Blocks.Add(new Paragraph(new Run(
            $"Patient: {patient.Vorname} {patient.Nachname}\n" +
            $"Geburtsdatum: {patient.Geburtsdatum:dd.MM.yyyy}\n" +
            $"Datum: {DateTime.Now:dd.MM.yyyy}"
        ))
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 20)
        });

        AddSection("Diagnosen", diagnoses.Select(x =>
            $"{x.CatalogItem.Code} - {x.CatalogItem.Name}" +
            (string.IsNullOrWhiteSpace(x.Notes) ? "" : $" | {x.Notes}")));

        AddSection("Medikation", medications.Select(x =>
            $"{x.CatalogItem.Name} | Dosierung: {x.Dosage}" +
            (string.IsNullOrWhiteSpace(x.Notes) ? "" : $" | {x.Notes}")));

        AddTextSection("Befund", "Bitte Befundtext hier ergänzen...");
        AddTextSection("Therapie / Empfehlung", "Bitte Therapieempfehlung hier ergänzen...");

        LetterDocument.Blocks.Add(new Paragraph(new Run(
            "\nMit freundlichen Grüßen\n\n" +
            "______________________________\n" +
            "Unterschrift Arzt"
        ))
        {
            Margin = new Thickness(0, 30, 0, 0)
        });
    }

    private void AddSection(string title, IEnumerable<string> items)
    {
        LetterDocument.Blocks.Add(new Paragraph(new Run(title))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 10, 0, 5)
        });

        var list = new List();

        foreach (var item in items)
        {
            list.ListItems.Add(new ListItem(new Paragraph(new Run(item))));
        }

        if (!list.ListItems.Any())
            LetterDocument.Blocks.Add(new Paragraph(new Run("Keine Einträge vorhanden.")));
        else
            LetterDocument.Blocks.Add(list);
    }

    private void AddTextSection(string title, string text)
    {
        LetterDocument.Blocks.Add(new Paragraph(new Run(title))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 15, 0, 5)
        });

        LetterDocument.Blocks.Add(new Paragraph(new Run(text)));
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();

        if (dialog.ShowDialog() == true)
        {
            dialog.PrintDocument(
                ((IDocumentPaginatorSource)LetterDocument).DocumentPaginator,
                "Arztbrief");
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}