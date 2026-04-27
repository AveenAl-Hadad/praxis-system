using Praxis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Praxis.Application.Interfaces
{
    public interface ILaborService
    {
        Task<List<LaborRecord>> GetAllAsync();
        Task<LaborRecord?> GetByIdAsync(int laborId);
        Task AddAsync(LaborRecord record);
        Task AssignToPatientAsync(int laborId, int patientId);
        Task SetStatusAsync(int laborId, string status);
        Task MarkAddedToMedicalRecordAsync(int laborId);

    }
}
