using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces
{ 
public interface IDashboardTaskService
{
    Task<List<DashboardTask>> GetOpenTasksAsync();
    Task<List<DashboardTask>> GetDueTasksAsync(DateTime date);
    Task<DashboardTask?> GetByIdAsync(int id);
    Task AddTaskAsync(DashboardTask task);
    Task UpdateTaskAsync(DashboardTask task);
    Task MarkAsDoneAsync(int taskId);
}
}