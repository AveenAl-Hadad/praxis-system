using System.Windows;
using Praxis.Domain.Entities;
using Praxis.Client.Views;

namespace Praxis.Client.Logic.UI;

public class WpfDialogService : IDialogService
{
    public bool TryCreatePatient(Window owner, out Patient? patient)
    {
        var dlg = new AddPatientWindow
        {
            Owner = owner
        };

        if (dlg.ShowDialog() == true && dlg.CreatedPatient != null)
        {
            patient = dlg.CreatedPatient;
            return true;
        }

        patient = null;
        return false;
    }

    public bool TryEditPatient(Window owner, Patient selected, out Patient? updated)
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

        var dlg = new AddPatientWindow(copy)
        {
            Owner = owner
        };

        if (dlg.ShowDialog() == true && dlg.CreatedPatient != null)
        {
            updated = dlg.CreatedPatient;
            return true;
        }

        updated = null;
        return false;
    }
}