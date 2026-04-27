using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Praxis.Domain.Entities;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Praxis.Client.Views.Pages.Patienten;

public partial class PatientDocumentsPage : System.Windows.Controls.UserControl
{
    private readonly ObservableCollection<PatientDocument> _documents = new();

    private Patient? _currentPatient;
    private PatientDocument? _selectedDocument;

    public PatientDocumentsPage()
    {
        InitializeComponent();

        DocumentsGrid.ItemsSource = _documents;
        DocumentTypeComboBox.SelectedIndex = 6;
    }

    public async Task LoadPatientAsync(Patient patient)
    {
        _currentPatient = patient;

        PatientNameTextBox.Text = patient.FullName;
        GeburtsdatumTextBox.Text = patient.Geburtsdatum.ToString("dd.MM.yyyy");
        TelefonTextBox.Text = patient.Telefonnummer;
        EmailTextBox.Text = patient.Email;

        ClearForm();

        await LoadDocumentsAsync();
    }

    private async Task LoadDocumentsAsync()
    {
        if (_currentPatient == null)
            return;

        try
        {
            _documents.Clear();

            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            var documents = await mainWindow.GetDocumentsByPatientIdAsync(_currentPatient.Id);

            foreach (var document in documents)
                _documents.Add(document);

            DocumentCountTextBlock.Text = $"{_documents.Count} Dokument(e)";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Dokumente konnten nicht geladen werden:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void AddDocumentButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPatient == null)
        {
            MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Dokument auswählen",
            Filter = "Alle unterstützten Dateien|*.pdf;*.jpg;*.jpeg;*.png;*.doc;*.docx;*.txt|PDF-Dateien|*.pdf|Bilder|*.jpg;*.jpeg;*.png|Word-Dateien|*.doc;*.docx|Textdateien|*.txt|Alle Dateien|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var sourceFile = dialog.FileName;
            var originalFileName = Path.GetFileName(sourceFile);

            var targetDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "PraxisSystem",
                "PatientDocuments",
                _currentPatient.Id.ToString());

            Directory.CreateDirectory(targetDirectory);

            var safeFileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{originalFileName}";
            var targetFile = Path.Combine(targetDirectory, safeFileName);

            File.Copy(sourceFile, targetFile, overwrite: false);

            var documentType = GetSelectedDocumentType();

            var title = string.IsNullOrWhiteSpace(TitleTextBox.Text)
                ? Path.GetFileNameWithoutExtension(originalFileName)
                : TitleTextBox.Text.Trim();

            var document = new PatientDocument
            {
                PatientId = _currentPatient.Id,
                Title = title,
                DocumentType = documentType,
                FileName = originalFileName,
                FilePath = targetFile,
                Description = NullIfEmpty(DescriptionTextBox.Text),
                CreatedAt = DateTime.Now,
                UploadDate = DateTime.Now
            };

            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                await mainWindow.AddDocumentAsync(document);

            ClearForm();
            await LoadDocumentsAsync();

            MessageBox.Show("Dokument wurde hinzugefügt.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Dokument konnte nicht hinzugefügt werden:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void DocumentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DocumentsGrid.SelectedItem is not PatientDocument document)
            return;

        _selectedDocument = document;

        TitleTextBox.Text = document.Title;
        DescriptionTextBox.Text = document.Description ?? string.Empty;

        SetDocumentType(document.DocumentType);
    }

    private async void EditDocumentButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedDocument == null)
        {
            MessageBox.Show("Bitte zuerst ein Dokument auswählen.");
            return;
        }

        try
        {
            _selectedDocument.Title = string.IsNullOrWhiteSpace(TitleTextBox.Text)
                ? _selectedDocument.FileName
                : TitleTextBox.Text.Trim();

            _selectedDocument.DocumentType = GetSelectedDocumentType();
            _selectedDocument.Description = NullIfEmpty(DescriptionTextBox.Text);

            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                await mainWindow.UpdateDocumentAsync(_selectedDocument);

            await LoadDocumentsAsync();
            ClearForm();

            MessageBox.Show("Dokument wurde aktualisiert.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Dokument konnte nicht bearbeitet werden:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void DeleteDocumentButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedDocument == null)
        {
            MessageBox.Show("Bitte zuerst ein Dokument auswählen.");
            return;
        }

        var result = MessageBox.Show(
            "Dokument wirklich löschen?\n\nDie Datenbank-Zuordnung wird gelöscht. Die Datei wird ebenfalls entfernt, wenn sie noch existiert.",
            "Löschen bestätigen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            var filePath = _selectedDocument.FilePath;
            var documentId = _selectedDocument.Id;

            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                await mainWindow.DeleteDocumentAsync(documentId);

            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
                File.Delete(filePath);

            ClearForm();
            await LoadDocumentsAsync();

            MessageBox.Show("Dokument wurde gelöscht.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Dokument konnte nicht gelöscht werden:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OpenDocumentButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSelectedDocument();
    }

    private void DocumentsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        OpenSelectedDocument();
    }

    private void OpenSelectedDocument()
    {
        if (_selectedDocument == null)
        {
            MessageBox.Show("Bitte zuerst ein Dokument auswählen.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedDocument.FilePath) || !File.Exists(_selectedDocument.FilePath))
        {
            MessageBox.Show("Die Datei wurde nicht gefunden.");
            return;
        }

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = _selectedDocument.FilePath,
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Dokument konnte nicht geöffnet werden:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            await mainWindow.OpenPatientSearchPageAsync();
    }

    private void ClearForm()
    {
        _selectedDocument = null;
        DocumentsGrid.SelectedItem = null;

        TitleTextBox.Clear();
        DescriptionTextBox.Clear();
        DocumentTypeComboBox.SelectedIndex = 6;
    }

    private string GetSelectedDocumentType()
    {
        if (DocumentTypeComboBox.SelectedItem is ComboBoxItem item &&
            item.Content is string value)
        {
            return value;
        }

        return "Sonstiges";
    }

    private void SetDocumentType(string? documentType)
    {
        if (string.IsNullOrWhiteSpace(documentType))
        {
            DocumentTypeComboBox.SelectedIndex = 6;
            return;
        }

        foreach (ComboBoxItem item in DocumentTypeComboBox.Items)
        {
            if (string.Equals(item.Content?.ToString(), documentType, StringComparison.OrdinalIgnoreCase))
            {
                DocumentTypeComboBox.SelectedItem = item;
                return;
            }
        }

        DocumentTypeComboBox.SelectedIndex = 6;
    }

    private static string? NullIfEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}