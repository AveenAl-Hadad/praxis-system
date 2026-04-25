using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Praxis.Application.Interfaces
{
    public interface IMedicationImportService
    {
        Task ImportAsync(string filePath);
    }
}
