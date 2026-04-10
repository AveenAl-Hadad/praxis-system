using Praxis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Praxis.Infrastructure.Services
{
    public interface ILaborService
    {
        Task<List<LaborRecord>> GetAllAsync();
        Task AddAsync(LaborRecord record);
    }
}
