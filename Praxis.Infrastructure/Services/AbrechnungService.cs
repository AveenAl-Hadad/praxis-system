using Microsoft.EntityFrameworkCore;

using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;

namespace Praxis.Application.Services
{
    public class AbrechnungService : IAbrechnungService
    {
        private readonly PraxisDbContext _context;

        public AbrechnungService(PraxisDbContext context)
        {
            _context = context;
        }

        public async Task<List<Abrechnungsbeleg>> GetAllAsync()
        {
            return await _context.Abrechnungsbelegs.ToListAsync();
        }

        public async Task AddAsync(Abrechnungsbeleg record)
        {
            _context.Abrechnungsbelegs.Add(record);
            await _context.SaveChangesAsync();
        }
        public async Task<Abrechnungsbeleg> GetByIdAsync(int id)
        {
            return await _context.Abrechnungsbelegs.FindAsync(id);
        }

        public async Task UpdateAsync(Abrechnungsbeleg record)
        {
            _context.Abrechnungsbelegs.Update(record);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _context.Abrechnungsbelegs.FindAsync(id);
            if (item != null)
            {
                _context.Abrechnungsbelegs.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}