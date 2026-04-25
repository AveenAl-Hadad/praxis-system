using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface IPracticeSettingsService
{
    Task<PracticeSettings> GetAsync();
    Task SaveAsync(PracticeSettings settings);
}