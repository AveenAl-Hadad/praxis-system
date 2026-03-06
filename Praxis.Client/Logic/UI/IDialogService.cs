using Praxis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Praxis.Client.Logic.UI
{
    public interface IDialogService
    {
        //Gibt true zurück wenn user "Speichern" klickt, sonst false
        bool TryCreatePatient(Window owner, out Patient? patient);
        bool TryEditPatient(Window owner, Patient existing, out Patient? update);
        
    }
}
