using System.Windows;
using Praxis.Domain.Entities;
using Praxis.Client.Views;

namespace Praxis.Client.Logic.UI;

public class WpfDialogService : IDialogService
{
    // Öffnet Dialog zum Erstellen eines neuen Patienten
    public bool TryCreatePatient(Window owner, out Patient? patient)
    {
        // Neues Dialogfenster erstellen
        var dlg = new AddPatientWindow
        {
            Owner = owner // Owner setzen (Hauptfenster)
        };

        // Dialog anzeigen → true wenn "OK" gedrückt wurde
        if (dlg.ShowDialog() == true && dlg.CreatedPatient != null)
        {
            // Erstellten Patienten zurückgeben
            patient = dlg.CreatedPatient;
            return true;
        }

        // Falls abgebrochen → null zurückgeben
        patient = null;
        return false;
    }

    // Öffnet Dialog zum Bearbeiten eines bestehenden Patienten
    public bool TryEditPatient(Window owner, Patient selected, out Patient? updated)
    {
        // Kopie des bestehenden Patienten erstellen (wichtig!)
        var copy = new Patient
        {
            Id = selected.Id,
            Vorname = selected.Vorname,
            Nachname = selected.Nachname,
            Geburtsdatum = selected.Geburtsdatum,
            Email = selected.Email,
            Telefonnummer = selected.Telefonnummer,
            IsActive = selected.IsActive
        };

        // Dialog mit vorhandenen Daten öffnen
        var dlg = new AddPatientWindow(copy)
        {
            Owner = owner
        };

        // Dialog anzeigen → true wenn gespeichert wurde
        if (dlg.ShowDialog() == true && dlg.CreatedPatient != null)
        {
            // Geänderten Patienten zurückgeben
            updated = dlg.CreatedPatient;
            return true;
        }

        // Falls abgebrochen → null zurückgeben
        updated = null;
        return false;
    }
}