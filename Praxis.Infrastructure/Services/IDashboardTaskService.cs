using Praxis.Domain.Entities;

namespace Praxis.Infrastructure.Services;

public interface IDashboardTaskService
{
    Task<List<DashboardTask>> GetOpenTasksAsync();
    Task<List<DashboardTask>> GetDueTasksAsync(DateTime date);
    Task AddTaskAsync(DashboardTask task);
    Task UpdateTaskAsync(DashboardTask task);
    Task MarkAsDoneAsync(int taskId);
}