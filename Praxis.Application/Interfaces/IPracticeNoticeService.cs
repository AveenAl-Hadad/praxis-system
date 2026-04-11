using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces
{ 
public interface IPracticeNoticeService
{
    Task<List<PracticeNotice>> GetActiveNoticesAsync();
    Task AddNoticeAsync(PracticeNotice notice);
    Task UpdateNoticeAsync(PracticeNotice notice);
    Task DeactivateNoticeAsync(int noticeId);
    Task DeleteNoticeAsync(int noticeId);
    }
}