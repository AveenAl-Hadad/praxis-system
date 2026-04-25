using Praxis.Domain.Entities;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using Microsoft.Win32;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Praxis.Application.Interfaces;

using System.Windows.Media;

using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using MessageBox = System.Windows.MessageBox;
using Brushes = System.Windows.Media.Brushes;



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
        PrescriptionDocument.Blocks.Clear();

        var mainTable = new Table();
        mainTable.Columns.Add(new TableColumn { Width = new GridLength(220) });
        mainTable.Columns.Add(new TableColumn { Width = new GridLength(260) });

        var group = new TableRowGroup();
        mainTable.RowGroups.Add(group);

        var headerRow = new TableRow();
        group.Rows.Add(headerRow);

        var stampCell = new TableCell();
        headerRow.Cells.Add(stampCell);

        if (_practiceSettings != null)
        {
            stampCell.Blocks.Add(new Paragraph(new Run(
                $"{_practiceSettings.PracticeName}\n" +
                $"{_practiceSettings.DoctorName}\n" +
                $"{_practiceSettings.Street}\n" +
                $"{_practiceSettings.ZipCity}\n" +
                $"Tel: {_practiceSettings.Phone}\n" +
                $"E-Mail: {_practiceSettings.Email}"
            ))
            {
                FontSize = 11,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 20, 20)
            });
        }

        var titleCell = new TableCell();
        headerRow.Cells.Add(titleCell);

        titleCell.Blocks.Add(new Paragraph(new Run("Rezept"))
        {
            FontSize = 30,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Right
        });

        titleCell.Blocks.Add(new Paragraph(new Run("Medikamentenverordnung"))
        {
            FontSize = 14,
            TextAlignment = TextAlignment.Right
        });

        PrescriptionDocument.Blocks.Add(mainTable);

        var patientBox = new Paragraph(new Run(
            $"Patient: {patient.Vorname} {patient.Nachname}\n" +
            $"Geburtsdatum: {patient.Geburtsdatum:dd.MM.yyyy}\n" +
            $"Datum: {DateTime.Now:dd.MM.yyyy}"
        ))
        {
            FontSize = 13,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 10, 0, 20)
        };

        PrescriptionDocument.Blocks.Add(patientBox);

        PrescriptionDocument.Blocks.Add(new Paragraph(new Run("Verordnete Medikamente"))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var medTable = new Table
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1)
        };

        medTable.Columns.Add(new TableColumn { Width = new GridLength(200) });
        medTable.Columns.Add(new TableColumn { Width = new GridLength(120) });
        medTable.Columns.Add(new TableColumn { Width = new GridLength(220) });

        var medGroup = new TableRowGroup();
        medTable.RowGroups.Add(medGroup);

        var tableHeader = new TableRow
        {
            Background = Brushes.LightGray
        };

        medGroup.Rows.Add(tableHeader);

        tableHeader.Cells.Add(CreateCell("Medikament", true));
        tableHeader.Cells.Add(CreateCell("Dosierung", true));
        tableHeader.Cells.Add(CreateCell("Hinweis", true));

        foreach (var medication in medications)
        {
            var row = new TableRow();
            medGroup.Rows.Add(row);

            row.Cells.Add(CreateCell(medication.CatalogItem.Name));
            row.Cells.Add(CreateCell(medication.Dosage));
            row.Cells.Add(CreateCell(medication.Notes));
        }

        PrescriptionDocument.Blocks.Add(medTable);

        PrescriptionDocument.Blocks.Add(new Paragraph(new Run(
            "\n\nUnterschrift Arzt / Stempel: ______________________________"
        ))
        {
            FontSize = 13,
            Margin = new Thickness(0, 35, 0, 0)
        });
    }
    private static TableCell CreateCell(string text, bool bold = false)
    {
        var paragraph = new Paragraph(new Run(text ?? string.Empty))
        {
            Margin = new Thickness(0),
            Padding = new Thickness(6)
        };

        if (bold)
            paragraph.FontWeight = FontWeights.Bold;

        return new TableCell(paragraph)
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(4)
        };
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