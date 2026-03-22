using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

/// <summary>
/// Dieses Fenster dient zur Verwaltung von Dokumenten eines Patienten.
/// 
/// Funktionen:
/// - Dokumente eines Patienten anzeigen
/// - neue Dokumente hochladen
/// - gespeicherte Dokumente öffnen
/// </summary>
public partial class DocumentWindow : Window
{
    /// <summary>
    /// Service für Dokumentoperationen (laden, speichern).
    /// </summary>
    private readonly IDocumentService _documentService;

    /// <summary>
    /// Der aktuell ausgewählte Patient,
    /// dessen Dokumente angezeigt werden.
    /// </summary>
    private readonly Patient _patient;

    /// <summary>
    /// Konstruktor des Fensters.
    /// 
    /// Übergibt den Dokument-Service und den Patienten,
    /// dessen Dokumente verwaltet werden sollen.
    /// </summary>
    public DocumentWindow(IDocumentService documentService, Patient patient)
    {
        InitializeComponent();

        _documentService = documentService;
        _patient = patient;

        Loaded += DocumentWindow_Loaded;
    }

    /// <summary>
    /// Wird beim Laden des Fensters ausgeführt.
    /// 
    /// Lädt alle Dokumente des Patienten und zeigt sie im Grid an.
    /// </summary>
    private async void DocumentWindow_Loaded(object sender, RoutedEventArgs e)
    {
        DocumentsGrid.ItemsSource =
            await _documentService.GetDocumentsByPatientAsync(_patient.Id);
    }

    /// <summary>
    /// Öffnet einen Datei-Dialog, um ein neues Dokument auszuwählen.
    /// 
    /// Ablauf:
    /// - Benutzer wählt eine Datei aus
    /// - Datei wird in einen lokalen Ordner kopiert
    /// - Dokument wird in der Datenbank gespeichert
    /// - Liste wird aktualisiert
    /// </summary>
    private async void Upload_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog();

        // Prüfen, ob der Benutzer eine Datei ausgewählt hat
        if (dialog.ShowDialog() == true)
        {
            // Zielordner erstellen (z. B. documents/PatientId)
            var folder = Path.Combine("documents", _patient.Id.ToString());
            Directory.CreateDirectory(folder);

            // Dateiname und Zielpfad bestimmen
            var fileName = Path.GetFileName(dialog.FileName);
            var newPath = Path.Combine(folder, fileName);

            // Datei kopieren (überschreibt vorhandene Datei)
            File.Copy(dialog.FileName, newPath, true);

            // Dokument-Objekt erstellen
            var doc = new PatientDocument
            {
                PatientId = _patient.Id,
                FileName = fileName,
                FilePath = newPath
            };

            // In der Datenbank speichern
            await _documentService.AddDocumentAsync(doc);

            // Liste neu laden
            DocumentsGrid.ItemsSource =
                await _documentService.GetDocumentsByPatientAsync(_patient.Id);
        }
    }

    /// <summary>
    /// Öffnet das ausgewählte Dokument mit dem Standardprogramm des Systems.
    /// </summary>
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