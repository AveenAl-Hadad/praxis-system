using Praxis.Domain.Entities;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using Microsoft.Win32;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Praxis.Application.Interfaces;

using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using MessageBox = System.Windows.MessageBox;


namespace Praxis.Client.Views;

public partial class PrescriptionPreviewWindow : Window
{
    private readonly Patient _patient;
    private readonly List<PatientMedication> _medications;

    private readonly IPracticeSettingsService _practiceSettingsService;
    private PracticeSettings? _practiceSettings;
    public PrescriptionPreviewWindow(
                                        Patient patient,
                                        IEnumerable<PatientMedication> medications,
                                        IPracticeSettingsService practiceSettingsService)
    {
        InitializeComponent();

        _patient = patient;
        _medications = medications.ToList();
        _practiceSettingsService = practiceSettingsService;

        LoadSettingsAndBuild();
    }

    private async void LoadSettingsAndBuild()
    {
        _practiceSettings = await _practiceSettingsService.GetAsync();
        BuildDocument(_patient, _medications);
    }

    //PDF-Export Methode hinzufügen
    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF Datei (*.pdf)|*.pdf",
            FileName = $"Rezept-{_patient.Nachname}-{DateTime.Now:yyyy-MM-dd}.pdf"
        };

        if (dialog.ShowDialog() != true)
            return;

        var document = new PdfDocument();
        document.Info.Title = "Rezept";

        var page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;

        var gfx = XGraphics.FromPdfPage(page);

        var titleFont = new XFont("Arial", 20, XFontStyle.Bold);
        var headerFont = new XFont("Arial", 13, XFontStyle.Bold);
        var normalFont = new XFont("Arial", 11, XFontStyle.Regular);

        double y = 50;

        gfx.DrawString("Rezept / Medikamentenverordnung", titleFont, XBrushes.Black,
            new XRect(40, y, page.Width - 80, 30), XStringFormats.TopLeft);

        y += 45;

        gfx.DrawString($"Patient: {_patient.Vorname} {_patient.Nachname}", normalFont, XBrushes.Black, 40, y);
        y += 20;

        gfx.DrawString($"Geburtsdatum: {_patient.Geburtsdatum:dd.MM.yyyy}", normalFont, XBrushes.Black, 40, y);
        y += 20;

        gfx.DrawString($"Datum: {DateTime.Now:dd.MM.yyyy}", normalFont, XBrushes.Black, 40, y);
        y += 35;

        gfx.DrawString("Medikamente", headerFont, XBrushes.Black, 40, y);
        y += 25;

        foreach (var medication in _medications)
        {
            if (y > page.Height - 100)
            {
                page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                gfx = XGraphics.FromPdfPage(page);
                y = 50;
            }

            gfx.DrawString($"{medication.CatalogItem.Name}", headerFont, XBrushes.Black, 40, y);
            y += 18;

            gfx.DrawString($"Dosierung: {medication.Dosage}", normalFont, XBrushes.Black, 60, y);
            y += 18;

            if (!string.IsNullOrWhiteSpace(medication.Notes))
            {
                gfx.DrawString($"Notiz: {medication.Notes}", normalFont, XBrushes.Black, 60, y);
                y += 18;
            }

            y += 10;
        }

        y += 30;

        gfx.DrawString("Unterschrift Arzt: ______________________________", normalFont, XBrushes.Black, 40, y);

        document.Save(dialog.FileName);

        MessageBox.Show("PDF wurde gespeichert.");
    }
    private void BuildDocument(Patient patient, IEnumerable<PatientMedication> medications)
    {
        if (_practiceSettings != null)
        {
            PrescriptionDocument.Blocks.Add(new Paragraph(new Run(
                $"{_practiceSettings.PracticeName}\n" +
                $"{_practiceSettings.DoctorName}\n" +
                $"{_practiceSettings.Street}\n" +
                $"{_practiceSettings.ZipCity}\n" +
                $"Tel: {_practiceSettings.Phone}\n" +
                $"{_practiceSettings.Email}"
            ))
            {
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 20)
            });
        }
        PrescriptionDocument.Blocks.Clear();
        if (_practiceSettings != null)
        {
            PrescriptionDocument.Blocks.Add(new Paragraph(new Run(
                $"{_practiceSettings.PracticeName}\n" +
                $"{_practiceSettings.DoctorName}\n" +
                $"{_practiceSettings.Street}\n" +
                $"{_practiceSettings.ZipCity}\n" +
                $"Tel: {_practiceSettings.Phone}\n" +
                $"E-Mail: {_practiceSettings.Email}"
            ))
            {
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 25)
            });
        }

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