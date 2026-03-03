using System.Threading.Tasks;
using System.Windows;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Logic;

/// <summary>
/// Kapselt die CRUD-Aktionen für Patienten.
/// UI (Windows/Dialogs) wird hier geöffnet, DB-Operationen laufen über PatientService.
/// </summary>
public class PatientCrudController
{
    private readonly PatientService _patientService;

    public PatientCrudController(PatientService patientService)
    {
        _patientService = patientService;
    }

    /// <summary>Neuen Patienten anlegen (öffnet Dialog, speichert in DB).</summary>
    public async Task<bool> AddAsync(Window owner)
    {
        var dlg = new AddPatientWindow { Owner = owner };
        if (dlg.ShowDialog() == true && dlg.CreatedPatient != null)
        {
            await _patientService.AddPatientAsync(dlg.CreatedPatient);
            return true; // geändert
        }
        return false; // abgebrochen
    }

    /// <summary>Patient bearbeiten (öffnet Dialog mit Kopie, speichert Update).</summary>
    public async Task<bool> EditAsync(Window owner, Patient selected)
    {
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

        var dlg = new AddPatientWindow(copy) { Owner = owner };
        if (dlg.ShowDialog() == true && dlg.CreatedPatient != null)
        {
            await _patientService.UpdatePatientAsync(dlg.CreatedPatient);
            return true;
        }
        return false;
    }

    /// <summary>Patient löschen (mit Bestätigung).</summary>
    public async Task<bool> DeleteAsync(Patient selected)
    {
        var result = MessageBox.Show(
            $"Patient {selected.Nachname}, {selected.Vorname} wirklich löschen?",
            "Bestätigung",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return false;

        await _patientService.DeletePatientAsync(selected.Id);
        return true;
    }

    /// <summary>Status Aktiv/Inaktiv umschalten.</summary>
    public async Task<bool> ToggleActiveAsync(Patient selected)
    {
        await _patientService.ToggleActiveAsync(selected.Id);
        return true;
    }
}