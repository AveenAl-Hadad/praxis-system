namespace Praxis.Client.ViewModels
{
    public class DashboardTaskRow
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string Priority { get; set; } = "";
        public string DueDate { get; set; } = "";
        public string Status { get; set; } = "";
        public string AssignedTo { get; set; } = "";

        public string PriorityColor { get; set; } = "";
        public string StatusColor { get; set; } = "";
        public string DueDateColor { get; set; } = "";

        public bool IsCompleted { get; set; }
        public bool IsDueToday { get; set; }
        public bool IsOverdue { get; set; }

        public string Subtitle { get; set; } = "";
    }
}