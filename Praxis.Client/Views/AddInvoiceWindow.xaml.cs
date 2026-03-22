using System.Collections.ObjectModel;
using System.Windows;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

/// <summary>
/// Dieses Fenster dient zum Erstellen einer neuen Rechnung.
/// 
/// Der Benutzer kann:
/// - einen Patienten auswählen
/// - mehrere Rechnungspositionen (Leistungen) hinzufügen
/// - Menge und Preis pro Position festlegen
/// 
/// Am Ende wird eine fertige Rechnung erzeugt und zurückgegeben.
/// </summary>
public partial class AddInvoiceWindow : Window
{
    /// <summary>
    /// Service zum Laden der Patienten.
    /// </summary>
    private readonly IPatientService _patientService;

    /// <summary>
    /// Enthält die erstellte Rechnung nach dem Speichern.
    /// Wird vom aufrufenden Fenster ausgelesen.
    /// </summary>
    public Invoice? CreatedInvoice { get; private set; }

    /// <summary>
    /// Sammlung aller Rechnungspositionen (Leistungen).
    /// ObservableCollection sorgt dafür, dass die UI automatisch aktualisiert wird.
    /// </summary>
    private ObservableCollection<InvoiceItem> _items = new();

    /// <summary>
    /// Konstruktor des Fensters.
    /// 
    /// - Initialisiert die UI
    /// - setzt die Datenquelle für die Tabelle (ItemsGrid)
    /// - lädt beim Öffnen die Patienten
    /// </summary>
    public AddInvoiceWindow(IPatientService patientService)
    {
        InitializeComponent();
        _patientService = patientService;

        // Items (Rechnungspositionen) an die Tabelle binden
        ItemsGrid.ItemsSource = _items;

        // Event beim Laden des Fensters
        Loaded += AddInvoiceWindow_Loaded;
    }

    /// <summary>
    /// Wird beim Laden des Fensters ausgeführt.
    /// 
    /// Lädt alle Patienten und zeigt sie in der ComboBox an.
    /// </summary>
    private async void AddInvoiceWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var patients = await _patientService.GetAllPatientsAsync();
        PatientComboBox.ItemsSource = patients.ToList();
    }

    /// <summary>
    /// Fügt eine neue Rechnungsposition hinzu.
    /// 
    /// Es werden folgende Eingaben geprüft:
    /// - Beschreibung darf nicht leer sein
    /// - Menge muss > 0 sein
    /// - Preis darf nicht negativ sein
    /// </summary>
    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        // Beschreibung prüfen
        if (string.IsNullOrWhiteSpace(DescriptionBox.Text))
        {
            MessageBox.Show("Bitte Beschreibung eingeben.");
            return;
        }

        // Menge prüfen
        if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity <= 0)
        {
            MessageBox.Show("Ungültige Menge.");
            return;
        }

        // Preis prüfen
        if (!decimal.TryParse(UnitPriceBox.Text, out decimal unitPrice) || unitPrice < 0)
        {
            MessageBox.Show("Ungültiger Preis.");
            return;
        }

        // Neue Rechnungsposition erstellen und zur Liste hinzufügen
        _items.Add(new InvoiceItem
        {
            Description = DescriptionBox.Text.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice
        });

        // Gesamtsumme neu berechnen
        UpdateTotal();

        // Eingabefelder zurücksetzen
        DescriptionBox.Clear();
        QuantityBox.Clear();
        UnitPriceBox.Clear();
    }

    /// <summary>
    /// Entfernt die aktuell ausgewählte Rechnungsposition aus der Liste.
    /// </summary>
    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsGrid.SelectedItem is InvoiceItem item)
        {
            _items.Remove(item);
            UpdateTotal();
        }
    }

    /// <summary>
    /// Berechnet die Gesamtsumme der Rechnung
    /// und aktualisiert die Anzeige im UI.
    /// </summary>
    private void UpdateTotal()
    {
        var total = _items.Sum(i => i.TotalPrice);

        // Anzeige der Summe im Format "123,45 €"
        TotalText.Text = $"Gesamt: {total:N2} €";

        // Tabelle aktualisieren (z. B. bei Änderungen)
        ItemsGrid.Items.Refresh();
    }

    /// <summary>
    /// Speichert die Rechnung.
    /// 
    /// - Prüft, ob ein Patient ausgewählt wurde
    /// - Prüft, ob mindestens eine Position vorhanden ist
    /// - Erstellt ein neues Invoice-Objekt
    /// </summary>
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Patient prüfen
        if (PatientComboBox.SelectedItem is not Patient patient)
        {
            MessageBox.Show("Bitte einen Patienten auswählen.");
            return;
        }

        // Prüfen, ob mindestens eine Position vorhanden ist
        if (_items.Count == 0)
        {
            MessageBox.Show("Bitte mindestens eine Leistung hinzufügen.");
            return;
        }

        // Neue Rechnung erstellen
        CreatedInvoice = new Invoice
        {
            PatientId = patient.Id,
            InvoiceDate = DateTime.Now,

            // Beispiel: RE-20260322153000
            InvoiceNumber = $"RE-{DateTime.Now:yyyyMMddHHmmss}",

            Items = _items.ToList(),
            TotalAmount = _items.Sum(i => i.TotalPrice)
        };

        // Fenster erfolgreich schließen
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Bricht das Erstellen der Rechnung ab.
    /// Fenster wird ohne Speicherung geschlossen.
    /// </summary>
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}