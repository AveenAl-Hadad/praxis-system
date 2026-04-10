using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Infrastructure.Services;

namespace Praxis.Infrastructure.Services;
    public class LaborService : ILaborService
    {
        private readonly PraxisDbContext _context;

        public LaborService(PraxisDbContext context)
        {
            _context = context;
        }

        public async Task<List<LaborRecord>> GetAllAsync()
        {
            return await _context.LaborRecords.ToListAsync();
        }

        public async Task AddAsync(LaborRecord record)
        {
            _context.LaborRecords.Add(record);
            await _context.SaveChangesAsync();
        }
    }
