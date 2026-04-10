using Praxis.Domain.Entities;

namespace Praxis.Infrastructure.Services;

public interface IPracticeNoticeService
{
    Task<List<PracticeNotice>> GetActiveNoticesAsync();
    Task AddNoticeAsync(PracticeNotice notice);
    Task UpdateNoticeAsync(PracticeNotice notice);
    Task DeactivateNoticeAsync(int noticeId);
}