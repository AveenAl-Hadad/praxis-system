using System.Threading.Tasks;
using System.Windows;
using Praxis.Client.Views;
using Praxis.Client.Logic.UI;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Logic;

/// <summary>
/// Kapselt die CRUD-Aktionen für Patienten.
/// UI (Windows/Dialogs) wird hier geöffnet, DB-Operationen laufen über PatientService.
/// </summary>
public class PatientCrudController
{
    private readonly IPatientService _patientService;
    private readonly IDialogService _dialogs;
    private readonly IMessageBoxService _messages;

    public PatientCrudController(IPatientService patientService, IDialogService dialogs, IMessageBoxService messages)
    {
        _patientService = patientService;
        _dialogs = dialogs;
        _messages = messages;
    }



    /// <summary>Neuen Patienten anlegen (öffnet Dialog, speichert in DB).</summary>
    public async Task<bool> AddAsync(Window owner)
    {
        try
        {
            if (!_dialogs.TryCreatePatient(owner, out var patient) || patient == null)
                return false;

            await _patientService.AddPatientAsync(patient);
            return true;
        }
        catch (Exception ex)
        {
            _messages.ShowError(ex.Message, "Fehler");
            return false;
        }
    }

    /// <summary>Patient bearbeiten (öffnet Dialog mit Kopie, speichert Update).</summary>
    public async Task<bool> EditAsync(Window owner, Patient selected)
    {
        try
        {
            if (!_dialogs.TryEditPatient(owner, selected, out var updated) || updated == null)
                return false;

            await _patientService.UpdatePatientAsync(updated);
            return true;
        }
        catch (Exception ex)
        {
            _messages.ShowError(ex.Message, "Fehler");
            return false;
        }
    }

    /// <summary>Patient löschen (mit Bestätigung).</summary>
    public async Task<bool> DeleteAsync(Patient selected)
    {
        var result = _messages.Confirm(
            $"Patient {selected.Nachname}, {selected.Vorname} wirklich löschen?",
            "Bestätigung");

        if (result != MessageBoxResult.Yes)
            return false;

        try
        {
            await _patientService.DeletePatientAsync(selected.Id);
            return true;
        }
        catch (Exception ex)
        {
            _messages.ShowError(ex.Message, "Fehler");
            return false;
        }
    }

    /// <summary>Status Aktiv/Inaktiv umschalten.</summary>
    public async Task<bool> ToggleActiveAsync(Patient selected)
    {
        try
        {
            await _patientService.ToggleActiveAsync(selected.Id);
            return true;
        }
        catch (Exception ex)
        {
            _messages.ShowError(ex.Message, "Fehler");
            return false;
        }
    }
}
