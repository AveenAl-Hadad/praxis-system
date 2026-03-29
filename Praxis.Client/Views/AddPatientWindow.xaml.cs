using System;
using System.Text.RegularExpressions;
using System.Windows;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

/// <summary>
/// Dieses Fenster dient zum Erstellen eines neuen Patienten
/// oder zum Bearbeiten eines bereits vorhandenen Patienten.
/// 
/// Der Benutzer kann persönliche Daten wie Vorname, Nachname,
/// Geburtsdatum, E-Mail und Telefonnummer eingeben.
/// </summary>
public partial class AddPatientWindow : Window
{
    /// <summary>
    /// Enthält den neu erstellten oder bearbeiteten Patienten.
    /// Diese Eigenschaft kann nach dem Schließen des Fensters
    /// vom aufrufenden Code ausgelesen werden.
    /// </summary>
    public Patient? CreatedPatient { get; private set; }

    /// <summary>
    /// Konstruktor des Fensters.
    /// 
    /// Wenn ein Patient übergeben wird, arbeitet das Fenster
    /// im Bearbeitungsmodus und füllt die Eingabefelder mit
    /// den vorhandenen Daten.
    /// 
    /// Wenn kein Patient übergeben wird, wird ein neuer Patient
    /// angelegt und der Status standardmäßig auf aktiv gesetzt.
    /// </summary>
    /// <param name="patient">Optional: Ein vorhandener Patient zum Bearbeiten.</param>
    public AddPatientWindow(Patient? patient = null)
    {
        InitializeComponent();

        // Bearbeitungsmodus:
        // Vorhandene Patientendaten in die Eingabefelder laden
        if (patient != null)
        {
            FirstNameBox.Text = patient.Vorname;
            LastNameBox.Text = patient.Nachname;
            DobPicker.SelectedDate = patient.Geburtsdatum;
            EmailBox.Text = patient.Email;
            PhoneBox.Text = patient.Telefonnummer;

            IsActiveCheck.IsChecked = patient.IsActive;
            CreatedPatient = patient;
        }
        else
        {
            // Standardwert bei neuem Patienten
            IsActiveCheck.IsChecked = true;
        }
    }

    /// <summary>
    /// Bricht den Vorgang ab und schließt das Fenster ohne zu speichern.
    /// </summary>
    /// <param name="sender">Das auslösende Objekt.</param>
    /// <param name="e">Eventdaten des Click-Events.</param>
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Speichert die eingegebenen Patientendaten.
    /// 
    /// Ablauf:
    /// - Eingaben werden zuerst validiert
    /// - falls nötig, wird ein neuer Patient erstellt
    /// - die Werte werden in das Patient-Objekt übernommen
    /// - das Fenster wird erfolgreich geschlossen
    /// - anschließend wird das Dashboard im Hauptfenster aktualisiert
    /// </summary>
    /// <param name="sender">Das auslösende Objekt.</param>
    /// <param name="e">Eventdaten des Click-Events.</param>
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        // Eingaben prüfen
        if (!ValidateInputs(out var error))
        {
            MessageBox.Show(error, "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Eingaben bereinigen (Leerzeichen entfernen)
        var lastName = (LastNameBox.Text ?? "").Trim();
        var firstName = (FirstNameBox.Text ?? "").Trim();
        var email = (EmailBox.Text ?? "").Trim();
        var phone = (PhoneBox.Text ?? "").Trim();

        // Wenn noch kein Patient existiert, neuen Patienten anlegen
        if (CreatedPatient == null)
        {
            CreatedPatient = new Patient();
        }

        // Werte in das Patient-Objekt übernehmen
        CreatedPatient.Vorname = firstName;
        CreatedPatient.Nachname = lastName;
        CreatedPatient.Geburtsdatum = DobPicker.SelectedDate ?? DateTime.Today;
        CreatedPatient.Email = string.IsNullOrWhiteSpace(email) ? string.Empty : email;
        CreatedPatient.Telefonnummer = string.IsNullOrWhiteSpace(phone) ? string.Empty : phone;
        CreatedPatient.IsActive = IsActiveCheck.IsChecked == true;

        // Fenster erfolgreich schließen
        DialogResult = true;
        Close();

        // Dashboard im Hauptfenster aktualisieren
        await ((MainWindow)Application.Current.MainWindow).LoadDashboardAsync();
    }

    /// <summary>
    /// Prüft alle Eingabefelder auf Gültigkeit.
    /// 
    /// Geprüft werden:
    /// - Vorname darf nicht leer sein
    /// - Nachname darf nicht leer sein
    /// - Geburtsdatum muss angegeben sein
    /// - Geburtsdatum darf nicht in der Zukunft liegen
    /// - E-Mail muss ein gültiges Format haben
    /// - Telefonnummer darf nur erlaubte Zeichen enthalten
    /// 
    /// Bei einem Fehler wird eine passende Fehlermeldung zurückgegeben.
    /// </summary>
    /// <param name="error">Gibt die Fehlermeldung zurück, falls die Validierung fehlschlägt.</param>
    /// <returns>True, wenn alle Eingaben gültig sind, sonst false.</returns>
    private bool ValidateInputs(out string error)
    {
        error = "";

        var firstName = (FirstNameBox.Text ?? "").Trim();
        var lastName = (LastNameBox.Text ?? "").Trim();
        var email = (EmailBox.Text ?? "").Trim();
        var phone = (PhoneBox.Text ?? "").Trim();
        var dob = DobPicker.SelectedDate;

        // Vorname prüfen
        //if (string.IsNullOrWhiteSpace(firstName))
        //{
        //    error = "Vorname ist ein Pflichtfeld.";
        //    FirstNameBox.Focus();
        //    return false;
        //}

        // Nachname prüfen
        if (string.IsNullOrWhiteSpace(lastName))
        {
            error = "Nachname ist ein Pflichtfeld.";
            LastNameBox.Focus();
            return false;
        }

        // Geburtsdatum prüfen
        if (!dob.HasValue)
        {
            error = "Geburtsdatum ist ein Pflichtfeld.";
            DobPicker.Focus();
            return false;
        }

        // Geburtsdatum darf nicht in der Zukunft liegen
        if (dob.Value.Date > DateTime.Today)
        {
            error = "Geburtsdatum darf nicht in der Zukunft liegen.";
            DobPicker.Focus();
            return false;
        }

        // E-Mail prüfen, falls etwas eingegeben wurde
        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(email))
            {
                error = "E-Mail ist ungültig.";
                EmailBox.Focus();
                return false;
            }
        }

        // Telefonnummer prüfen, falls etwas eingegeben wurde
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var phoneRegex = new Regex(@"^[0-9+\-\s()/]+$");
            if (!phoneRegex.IsMatch(phone))
            {
                error = "Telefonnummer enthält ungültige Zeichen.";
                PhoneBox.Focus();
                return false;
            }
        }

        // Alle Eingaben sind gültig
        return true;
    }
}