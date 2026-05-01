namespace Praxis.Client.ViewModels
{
    public class DashboardAppointmentRow
    {
        public string Time { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string Reason { get; set; } = "";
        public string Status { get; set; } = "";
        public int DurationMinutes { get; set; }
    }
}