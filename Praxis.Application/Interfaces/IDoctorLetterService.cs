using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface IDoctorLetterService
{
    Task<List<DoctorLetter>> GetAllAsync();
    Task AddAsync(DoctorLetter letter);
    Task DeleteAsync(int id);
}