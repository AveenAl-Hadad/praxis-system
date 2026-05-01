using Praxis.Client.Session;
using Praxis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Praxis.Client.Views.Main
{
    public partial class MainWindow
    {
        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            return await _dashboardService.GetStatsAsync();
        }
        public async Task<IEnumerable<Appointment>> GetAppointmentsByDateAsync(DateTime date)
        {
            return await _appointmentService.GetAppointmentsByDateAsync(date);
        }
        public async Task<IEnumerable<DashboardTask>> GetOpenDashboardTasksAsync()
        {
            return await _dashboardTaskService.GetOpenTasksAsync();
        }
        public async Task<IEnumerable<PracticeNotice>> GetActivePracticeNoticesAsync()
        {
            if (_practiceNoticeService == null)
                return Enumerable.Empty<PracticeNotice>();

            return await _practiceNoticeService.GetActiveNoticesAsync();
        }
        public async Task AddDashboardTaskAsync(DashboardTask task)
        {
            await _dashboardTaskService.AddTaskAsync(task);
            await RefreshBottomStatusAsync();
        }
        public async Task AddPracticeNoticeAsync(PracticeNotice notice)
        {
            await _practiceNoticeService.AddNoticeAsync(notice);
        }
        public async Task DeactivatePracticeNoticeAsync(int noticeId)
        {
            await _practiceNoticeService.DeactivateNoticeAsync(noticeId);
        }
        public async Task MarkDashboardTaskAsDoneAsync(int taskId)
        {
            await _dashboardTaskService.MarkAsDoneAsync(taskId);
            await RefreshBottomStatusAsync();
        }
        public async Task<IEnumerable<DashboardTask>> GetAllDashboardTasksAsync()
        {
            return await _dashboardTaskService.GetAllTasksAsync();
        }
        public async Task<DashboardTask?> GetDashboardTaskByIdAsync(int taskId)
        {
            return await _dashboardTaskService.GetByIdAsync(taskId);
        }
        public async Task UpdateDashboardTaskAsync(DashboardTask task)
        {
            await _dashboardTaskService.UpdateTaskAsync(task);
        }
        public async Task UpdatePracticeNoticeAsync(PracticeNotice notice)
        {
            await _practiceNoticeService.UpdateNoticeAsync(notice);
        }
        public async Task DeleteDashboardTaskAsync(int taskId)
        {
            await _dashboardTaskService.DeleteTaskAsync(taskId);
            await RefreshBottomStatusAsync();
        }
        public async Task MoveDashboardTaskToOpenAsync(int taskId)
        {
            var task = await _dashboardTaskService.GetByIdAsync(taskId);
            if (task == null)
                throw new InvalidOperationException("Aufgabe wurde nicht gefunden.");

            task.Status = "Offen";

            if (task.DueDate != null && task.DueDate.Value.Date <= DateTime.Today)
            {
                task.DueDate = DateTime.Today.AddDays(1);
            }

            await _dashboardTaskService.UpdateTaskAsync(task);
        }
        public async Task DeletePracticeNoticeAsync(int noticeId)
        {
            await _practiceNoticeService.DeleteNoticeAsync(noticeId);
        }
        public async Task<List<string>> GetDashboardWidgetOrderAsync()
        {
            var username = GetCurrentDashboardUsername();
            return await _dashboardLayoutService.GetWidgetOrderAsync(username);
        }
        public async Task SaveDashboardWidgetOrderAsync(List<string> widgetOrder)
        {
            var username = GetCurrentDashboardUsername();
            await _dashboardLayoutService.SaveWidgetOrderAsync(username, widgetOrder);
        }
        private string GetCurrentDashboardUsername()
        {
            return UserSession.CurrentUser?.Username ?? "default";
        }
    }
}
