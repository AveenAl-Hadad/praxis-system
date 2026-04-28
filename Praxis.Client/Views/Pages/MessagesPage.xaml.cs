using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using MessageBox = System.Windows.MessageBox;
using PrintDialog = System.Windows.Controls.PrintDialog;

using Microsoft.Win32;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace Praxis.Client.Views.Pages;

public partial class MessagesPage : System.Windows.Controls.UserControl
{
    private readonly IPracticeMessageService _messageService;
    private readonly IPatientService _patientService;
    private readonly IPracticeNoticeService _noticeService;
    private readonly IDoctorLetterService _doctorLetterService;
    private readonly IExternalMessageService _externalMessageService;

    private const string CurrentUser = "MFA";

    public MessagesPage(
        IPracticeMessageService messageService,
        IPatientService patientService,
        IPracticeNoticeService noticeService,
        IDoctorLetterService doctorLetterService,
        IExternalMessageService externalMessageService)
    {
        InitializeComponent();

        _messageService = messageService;
        _patientService = patientService;
        _noticeService = noticeService;

        RecipientCombo.SelectedIndex = 0;
        PriorityCombo.SelectedIndex = 0;
        _doctorLetterService = doctorLetterService;
        _externalMessageService = externalMessageService;
    }

    public async Task RefreshAsync()
    {
        InboxGrid.ItemsSource = await _messageService.GetInboxAsync(CurrentUser);
        SentGrid.ItemsSource = await _messageService.GetSentAsync(CurrentUser);
    }

    private async Task RefreshNoticesAsync()
    {
        NotesGrid.ItemsSource = await _noticeService.GetActiveNoticesAsync();
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        var recipient = (RecipientCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
        var priority = (PriorityCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();

        if (string.IsNullOrWhiteSpace(recipient))
        {
            MessageBox.Show("Bitte Empfänger auswählen.");
            return;
        }

        if (string.IsNullOrWhiteSpace(SubjectText.Text))
        {
            MessageBox.Show("Bitte Betreff eingeben.");
            return;
        }

        if (string.IsNullOrWhiteSpace(BodyText.Text))
        {
            MessageBox.Show("Bitte Nachricht eingeben.");
            return;
        }

        int? patientId = null;

        if (PatientCombo.SelectedItem is Patient selectedPatient)
        {
            patientId = selectedPatient.Id;
        }

        var message = new PracticeMessage
        {
            Sender = CurrentUser,
            Recipient = recipient,
            Priority = priority ?? "Normal",
            Subject = SubjectText.Text.Trim(),
            Body = BodyText.Text.Trim(),
            PatientId = patientId
        };

        await _messageService.SendAsync(message);

        SubjectText.Clear();
        BodyText.Clear();
        PatientSearchText.Clear();
        PatientCombo.ItemsSource = null;

        MessageBox.Show("Nachricht wurde gesendet.");

        await RefreshAsync();
    }

    private async void InboxGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (InboxGrid.SelectedItem is not PracticeMessage message)
            return;

        InboxBodyText.Text = message.Body;

        if (!message.IsRead)
        {
            await _messageService.MarkAsReadAsync(message.Id);
            await RefreshAsync();
        }
    }

    private async void MarkRead_Click(object sender, RoutedEventArgs e)
    {
        if (InboxGrid.SelectedItem is not PracticeMessage message)
        {
            MessageBox.Show("Bitte zuerst eine Nachricht auswählen.");
            return;
        }

        await _messageService.MarkAsReadAsync(message.Id);
        await RefreshAsync();
    }

    private async void DeleteMessage_Click(object sender, RoutedEventArgs e)
    {
        if (InboxGrid.SelectedItem is not PracticeMessage message)
        {
            MessageBox.Show("Bitte zuerst eine Nachricht auswählen.");
            return;
        }

        var result = MessageBox.Show(
            "Möchtest du diese Nachricht wirklich löschen?",
            "Nachricht löschen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        await _messageService.DeleteAsync(message.Id);

        InboxBodyText.Clear();

        await RefreshAsync();
    }

    private async void SearchPatient_Click(object sender, RoutedEventArgs e)
    {
        var search = PatientSearchText.Text.Trim();

        if (string.IsNullOrWhiteSpace(search))
        {
            MessageBox.Show("Bitte Name, E-Mail oder Telefonnummer eingeben.");
            return;
        }

        var patients = await _patientService.SearchPatientsAsync(search);

        PatientCombo.ItemsSource = patients;

        if (patients.Count == 0)
        {
            MessageBox.Show("Kein Patient gefunden.");
            return;
        }

        PatientCombo.SelectedIndex = 0;
    }

    public void ShowInbox()
    {
        MessagesTabControl.SelectedIndex = 0;
    }

    public void ShowSent()
    {
        MessagesTabControl.SelectedIndex = 1;
    }

    public void ShowNewMessage()
    {
        MessagesTabControl.SelectedIndex = 2;
    }

   public async void ShowNotes()
    {
        MessagesTabControl.SelectedIndex = 4;
        await RefreshNoticesAsync();
    }

    
    // Notiz
    private async void SaveNote_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NoteTitleText.Text))
        {
            MessageBox.Show("Bitte Titel eingeben.");
            return;
        }

        if (string.IsNullOrWhiteSpace(NoteText.Text))
        {
            MessageBox.Show("Bitte Notiz eingeben.");
            return;
        }

        var notice = new PracticeNotice
        {
            Title = NoteTitleText.Text.Trim(),
            Content = NoteText.Text.Trim(),
            Category = "Info",
            IsPinned = false,
            IsActive = true,
            CreatedBy = CurrentUser
        };

        await _noticeService.AddNoticeAsync(notice);

        NoteTitleText.Clear();
        NoteText.Clear();

        await RefreshNoticesAsync();

        MessageBox.Show("Notiz wurde gespeichert.");
    }
    public async void ShowDoctorLetters()
    {
        MessagesTabControl.SelectedIndex = 5;
        await RefreshDoctorLettersAsync();
    }

    private void NotesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NotesGrid.SelectedItem is not PracticeNotice notice)
            return;

        NoteTitleText.Text = notice.Title;
        NoteText.Text = notice.Content;
    }

    private async void DeleteNote_Click(object sender, RoutedEventArgs e)
    {
        if (NotesGrid.SelectedItem is not PracticeNotice notice)
        {
            MessageBox.Show("Bitte zuerst eine Notiz auswählen.");
            return;
        }

        var result = MessageBox.Show(
            "Möchtest du diese Notiz wirklich löschen?",
            "Notiz löschen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        await _noticeService.DeleteNoticeAsync(notice.Id);

        NoteTitleText.Clear();
        NoteText.Clear();

        await RefreshNoticesAsync();
    }


    //Arzt Brief

    private async Task RefreshDoctorLettersAsync()
    {
        DoctorLettersGrid.ItemsSource = await _doctorLetterService.GetAllAsync();
    }

    private async void SaveDoctorLetter_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DoctorLetterSubjectText.Text))
        {
            MessageBox.Show("Bitte Betreff eingeben.");
            return;
        }

        if (string.IsNullOrWhiteSpace(DoctorLetterBodyText.Text))
        {
            MessageBox.Show("Bitte Text für Arztbrief eingeben.");
            return;
        }

        int? patientId = null;

        if (DoctorLetterPatientCombo.SelectedItem is Patient selectedPatient)
        {
            patientId = selectedPatient.Id;
        }

        var letter = new DoctorLetter
        {
            Subject = DoctorLetterSubjectText.Text.Trim(),
            Body = DoctorLetterBodyText.Text.Trim(),
            CreatedBy = CurrentUser,
            PatientId = patientId
        };

        await _doctorLetterService.AddAsync(letter);

        DoctorLetterPatientSearchText.Clear();
        DoctorLetterPatientCombo.ItemsSource = null;
        DoctorLetterSubjectText.Clear();
        DoctorLetterBodyText.Clear();

        await RefreshDoctorLettersAsync();

        MessageBox.Show("Arztbrief wurde gespeichert.");
    }

    private void DoctorLettersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DoctorLettersGrid.SelectedItem is not DoctorLetter letter)
            return;

        DoctorLetterSubjectText.Text = letter.Subject;
        DoctorLetterBodyText.Text = letter.Body;

        if (letter.Patient != null)
        {
            DoctorLetterPatientSearchText.Text = letter.Patient.FullName;
            DoctorLetterPatientCombo.ItemsSource = new List<Patient> { letter.Patient };
            DoctorLetterPatientCombo.SelectedIndex = 0;
        }
        else
        {
            DoctorLetterPatientSearchText.Clear();
            DoctorLetterPatientCombo.ItemsSource = null;
        }
    }

    private async void DeleteDoctorLetter_Click(object sender, RoutedEventArgs e)
    {
        if (DoctorLettersGrid.SelectedItem is not DoctorLetter letter)
        {
            MessageBox.Show("Bitte zuerst einen Arztbrief auswählen.");
            return;
        }

        await _doctorLetterService.DeleteAsync(letter.Id);

        DoctorLetterSubjectText.Clear();
        DoctorLetterBodyText.Clear();

        await RefreshDoctorLettersAsync();
    }

    private void PrintDoctorLetter_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DoctorLetterSubjectText.Text) ||
            string.IsNullOrWhiteSpace(DoctorLetterBodyText.Text))
        {
            MessageBox.Show("Bitte zuerst Arztbrief auswählen oder schreiben.");
            return;
        }

        var printDialog = new PrintDialog();

        if (printDialog.ShowDialog() != true)
            return;

        var document = new FlowDocument
        {
            PagePadding = new Thickness(50),
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = 13
        };

        document.Blocks.Add(new Paragraph(new Run("Praxissoftware"))
        {
            FontSize = 22,
            FontWeight = FontWeights.Bold
        });

        document.Blocks.Add(new Paragraph(new Run("Arztbrief"))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold
        });

        document.Blocks.Add(new Paragraph(new Run($"Betreff: {DoctorLetterSubjectText.Text}")));
        if (DoctorLetterPatientCombo.SelectedItem is Patient printPatient)
        {
            document.Blocks.Add(new Paragraph(new Run($"Patient: {printPatient.FullName}")));
        }
        document.Blocks.Add(new Paragraph(new Run($"Datum: {DateTime.Now:dd.MM.yyyy HH:mm}")));
        document.Blocks.Add(new Paragraph(new Run(" ")));
        document.Blocks.Add(new Paragraph(new Run(DoctorLetterBodyText.Text)));

        printDialog.PrintDocument(
            ((IDocumentPaginatorSource)document).DocumentPaginator,
            "Arztbrief");
    }
    //Such methode
    private async void SearchDoctorLetterPatient_Click(object sender, RoutedEventArgs e)
    {
        var search = DoctorLetterPatientSearchText.Text.Trim();

        if (string.IsNullOrWhiteSpace(search))
        {
            MessageBox.Show("Bitte Name, E-Mail oder Telefonnummer eingeben.");
            return;
        }

        var patients = await _patientService.SearchPatientsAsync(search);

        DoctorLetterPatientCombo.ItemsSource = patients;

        if (patients.Count == 0)
        {
            MessageBox.Show("Kein Patient gefunden.");
            return;
        }

        DoctorLetterPatientCombo.SelectedIndex = 0;
    }


    //Nachricht External
    private async Task RefreshExternalMessagesAsync()
    {
        ExternalMessagesGrid.ItemsSource = await _externalMessageService.GetAllAsync();
    }

    public async void ShowExternalMessages()
    {
        MessagesTabControl.SelectedIndex = 3;
        await RefreshExternalMessagesAsync();
    }

    private async void ExternalMessagesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ExternalMessagesGrid.SelectedItem is not ExternalMessage message)
            return;

        ExternalMessageBodyText.Text = message.Body;

        if (!message.IsRead)
        {
            await _externalMessageService.MarkAsReadAsync(message.Id);
            await RefreshExternalMessagesAsync();
        }
    }

    private async void CreateExternalTestMessage_Click(object sender, RoutedEventArgs e)
    {
        var message = new ExternalMessage
        {
            SenderName = "Patient Online",
            SenderEmail = "patient@example.de",
            Subject = "Terminfrage",
            Body = "Guten Tag, ich möchte gerne einen Termin vereinbaren.",
            Status = "Neu",
            IsRead = false
        };

        await _externalMessageService.AddAsync(message);
        await RefreshExternalMessagesAsync();

        MessageBox.Show("Test-Nachricht wurde erstellt.");
    }

    private async void MarkExternalRead_Click(object sender, RoutedEventArgs e)
    {
        if (ExternalMessagesGrid.SelectedItem is not ExternalMessage message)
        {
            MessageBox.Show("Bitte zuerst eine externe Nachricht auswählen.");
            return;
        }

        await _externalMessageService.MarkAsReadAsync(message.Id);
        await RefreshExternalMessagesAsync();
    }

    private async void MarkExternalProcessed_Click(object sender, RoutedEventArgs e)
    {
        if (ExternalMessagesGrid.SelectedItem is not ExternalMessage message)
        {
            MessageBox.Show("Bitte zuerst eine externe Nachricht auswählen.");
            return;
        }

        await _externalMessageService.MarkAsProcessedAsync(message.Id);
        await RefreshExternalMessagesAsync();
    }

    private async void DeleteExternalMessage_Click(object sender, RoutedEventArgs e)
    {
        if (ExternalMessagesGrid.SelectedItem is not ExternalMessage message)
        {
            MessageBox.Show("Bitte zuerst eine externe Nachricht auswählen.");
            return;
        }

        await _externalMessageService.DeleteAsync(message.Id);

        ExternalMessageBodyText.Clear();

        await RefreshExternalMessagesAsync();
    }

    // PdD Srztbrief
    private void ExportDoctorLetterPdf_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DoctorLetterSubjectText.Text) ||
            string.IsNullOrWhiteSpace(DoctorLetterBodyText.Text))
        {
            MessageBox.Show("Bitte zuerst Arztbrief auswählen oder schreiben.");
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Title = "Arztbrief als PDF speichern",
            Filter = "PDF-Datei (*.pdf)|*.pdf",
            FileName = $"Arztbrief_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
        };

        if (saveDialog.ShowDialog() != true)
            return;

        var document = new PdfDocument();
        document.Info.Title = DoctorLetterSubjectText.Text;

        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);

        var titleFont = new XFont("Arial", 18, XFontStyle.Bold);
        var subtitleFont = new XFont("Arial", 11, XFontStyle.Regular);
        var headerFont = new XFont("Arial", 13, XFontStyle.Bold);
        var normalFont = new XFont("Arial", 10, XFontStyle.Regular);
        var smallFont = new XFont("Arial", 8, XFontStyle.Regular);

        double margin = 45;
        double y = 40;
        double lineHeight = 16;
        double maxWidth = page.Width - margin * 2;

        // Praxis-Briefkopf
        gfx.DrawString("Praxis Musterarzt", titleFont, XBrushes.Black,
            new XRect(margin, y, maxWidth, lineHeight), XStringFormats.TopLeft);

        y += 22;

        gfx.DrawString("Dr. med. Max Mustermann · Facharzt für Allgemeinmedizin",
            subtitleFont, XBrushes.Black,
            new XRect(margin, y, maxWidth, lineHeight), XStringFormats.TopLeft);

        y += 16;

        gfx.DrawString("Musterstraße 12 · 12345 Musterstadt · Tel. 01234 / 56789 · praxis@example.de",
            smallFont, XBrushes.Black,
            new XRect(margin, y, maxWidth, lineHeight), XStringFormats.TopLeft);

        y += 25;

        gfx.DrawLine(XPens.Black, margin, y, page.Width - margin, y);
        y += 30;

        // Patientendaten
        if (DoctorLetterPatientCombo.SelectedItem is Patient patient)
        {
            gfx.DrawString("Patient", headerFont, XBrushes.Black,
                new XRect(margin, y, maxWidth, lineHeight), XStringFormats.TopLeft);

            y += 20;

            gfx.DrawString(patient.FullName, normalFont, XBrushes.Black,
                new XRect(margin, y, maxWidth, lineHeight), XStringFormats.TopLeft);
            y += lineHeight;

            gfx.DrawString($"Geburtsdatum: {patient.Geburtsdatum:dd.MM.yyyy}", normalFont, XBrushes.Black,
                new XRect(margin, y, maxWidth, lineHeight), XStringFormats.TopLeft);
            y += lineHeight;

            gfx.DrawString($"Telefon: {patient.Telefonnummer}", normalFont, XBrushes.Black,
                new XRect(margin, y, maxWidth, lineHeight), XStringFormats.TopLeft);
            y += lineHeight;

            gfx.DrawString($"E-Mail: {patient.Email}", normalFont, XBrushes.Black,
                new XRect(margin, y, maxWidth, lineHeight), XStringFormats.TopLeft);

            y += 30;
        }

        // Datum rechts
        gfx.DrawString($"Datum: {DateTime.Now:dd.MM.yyyy}",
            normalFont,
            XBrushes.Black,
            new XRect(margin, y, maxWidth, lineHeight),
            XStringFormats.TopRight);

        y += 35;

        // Betreff
        gfx.DrawString(DoctorLetterSubjectText.Text,
            headerFont,
            XBrushes.Black,
            new XRect(margin, y, maxWidth, lineHeight),
            XStringFormats.TopLeft);

        y += 30;

        // Text
        DrawWrappedText(
            document,
            ref page,
            ref gfx,
            DoctorLetterBodyText.Text,
            normalFont,
            margin,
            ref y,
            maxWidth,
            lineHeight);

        y += 40;

        gfx.DrawString("Mit freundlichen Grüßen",
            normalFont,
            XBrushes.Black,
            new XRect(margin, y, maxWidth, lineHeight),
            XStringFormats.TopLeft);

        y += 35;

        gfx.DrawString("Dr. med. Max Mustermann",
            normalFont,
            XBrushes.Black,
            new XRect(margin, y, maxWidth, lineHeight),
            XStringFormats.TopLeft);

        // Fußzeile
        gfx.DrawLine(XPens.LightGray, margin, page.Height - 45, page.Width - margin, page.Height - 45);

        gfx.DrawString("Praxis Musterarzt · Musterstraße 12 · 12345 Musterstadt",
            smallFont,
            XBrushes.Gray,
            new XRect(margin, page.Height - 35, maxWidth, lineHeight),
            XStringFormats.TopCenter);

        document.Save(saveDialog.FileName);

        MessageBox.Show("PDF wurde erfolgreich exportiert.");
    }

    // Hilfsmethode
    private void DrawWrappedText(
    PdfDocument document,
    ref PdfPage page,
    ref XGraphics gfx,
    string text,
    XFont font,
    double margin,
    ref double y,
    double maxWidth,
    double lineHeight)
    {
        var paragraphs = text.Replace("\r", "").Split('\n');

        foreach (var paragraph in paragraphs)
        {
            var words = paragraph.Split(' ');
            var line = "";

            foreach (var word in words)
            {
                var testLine = string.IsNullOrWhiteSpace(line)
                    ? word
                    : line + " " + word;

                var size = gfx.MeasureString(testLine, font);

                if (size.Width > maxWidth)
                {
                    gfx.DrawString(line, font, XBrushes.Black,
                        new XRect(margin, y, maxWidth, lineHeight),
                        XStringFormats.TopLeft);

                    y += lineHeight;
                    line = word;

                    if (y > page.Height - 80)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        y = 50;
                    }
                }
                else
                {
                    line = testLine;
                }
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                gfx.DrawString(line, font, XBrushes.Black,
                    new XRect(margin, y, maxWidth, lineHeight),
                    XStringFormats.TopLeft);

                y += lineHeight;
            }

            y += 8;
        }
    }
}