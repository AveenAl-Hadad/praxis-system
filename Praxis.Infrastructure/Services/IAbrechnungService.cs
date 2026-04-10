using Praxis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Praxis.Infrastructure.Services
{
    public interface IAbrechnungService
    {
        Task<List<Abrechnungsbeleg>> GetAllAsync();
        Task AddAsync(Abrechnungsbeleg record);
        Task<Abrechnungsbeleg> GetByIdAsync(int id);
        Task UpdateAsync(Abrechnungsbeleg record);
        Task DeleteAsync(int id);
    }
}
