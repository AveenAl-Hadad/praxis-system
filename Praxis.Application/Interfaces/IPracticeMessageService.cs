using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface IPracticeMessageService
{
    Task<List<PracticeMessage>> GetInboxAsync(string recipient);
    Task<List<PracticeMessage>> GetSentAsync(string sender);
    Task SendAsync(PracticeMessage message);
    Task MarkAsReadAsync(int id);
    Task DeleteAsync(int id);
}