using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;

public class DashboardTaskService : IDashboardTaskService
{
    private readonly PraxisDbContext _db;

    public DashboardTaskService(PraxisDbContext db)
    {
        _db = db;
    }

    public async Task<List<DashboardTask>> GetOpenTasksAsync()
    {
        return await _db.DashboardTasks
            .Include(t => t.Patient)
            .Where(t => t.Status != "Erledigt")
            .OrderByDescending(t => t.Priority == "Hoch")
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<DashboardTask>> GetDueTasksAsync(DateTime date)
    {
        var dayEnd = date.Date.AddDays(1);

        return await _db.DashboardTasks
            .Include(t => t.Patient)
            .Where(t => t.Status != "Erledigt")
            .Where(t => t.DueDate != null && t.DueDate < dayEnd)
            .OrderBy(t => t.DueDate)
            .ToListAsync();
    }

    public async Task<DashboardTask?> GetByIdAsync(int id)
    {
        return await _db.DashboardTasks
            .Include(t => t.Patient)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task AddTaskAsync(DashboardTask task)
    {
        _db.DashboardTasks.Add(task);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateTaskAsync(DashboardTask task)
    {
        var existing = await _db.DashboardTasks.FindAsync(task.Id);
        if (existing == null)
            throw new InvalidOperationException("Aufgabe wurde nicht gefunden.");

        existing.Title = task.Title;
        existing.Description = task.Description;
        existing.Status = task.Status;
        existing.Priority = task.Priority;
        existing.DueDate = task.DueDate;
        existing.PatientId = task.PatientId;
        existing.AssignedTo = task.AssignedTo;

        await _db.SaveChangesAsync();
    }

    public async Task MarkAsDoneAsync(int taskId)
    {
        var existing = await _db.DashboardTasks.FindAsync(taskId);
        if (existing == null)
            throw new InvalidOperationException("Aufgabe wurde nicht gefunden.");

        existing.Status = "Erledigt";
        await _db.SaveChangesAsync();
    }
}