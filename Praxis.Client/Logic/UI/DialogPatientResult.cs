using Praxis.Domain.Entities;

namespace Praxis.Client.Logic.UI
{
    public record class DialogPatientResult(bool Ok, Patient? Patient)
    {
        private Patient? patient;

       
    }
}
