using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

public partial class DocumentWindow : Window
{
    private readonly IDocumentService _documentService;

    private readonly Patient _patient;

    public DocumentWindow(IDocumentService documentService, Patient patient)
    {
        InitializeComponent();

        _documentService = documentService;
        _patient = patient;

        Loaded += DocumentWindow_Loaded;
    }

    private async void DocumentWindow_Loaded(object sender, RoutedEventArgs e)
    {
        DocumentsGrid.ItemsSource =
            await _documentService.GetDocumentsByPatientAsync(_patient.Id);
    }

    private async void Upload_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog();

        if (dialog.ShowDialog() == true)
        {
            var folder = Path.Combine("documents", _patient.Id.ToString());

            Directory.CreateDirectory(folder);

            var fileName = Path.GetFileName(dialog.FileName);

            var newPath = Path.Combine(folder, fileName);

            File.Copy(dialog.FileName, newPath, true);

            var doc = new PatientDocument
            {
                PatientId = _patient.Id,
                FileName = fileName,
                FilePath = newPath
            };

            await _documentService.AddDocumentAsync(doc);

            DocumentsGrid.ItemsSource =
                await _documentService.GetDocumentsByPatientAsync(_patient.Id);
        }
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (DocumentsGrid.SelectedItem is not PatientDocument doc)
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = doc.FilePath,
            UseShellExecute = true
        });
    }
}