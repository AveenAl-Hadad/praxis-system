namespace Praxis.Client.ViewModels
{
    public class PracticeNoticeRow
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Category { get; set; } = "";
        public string CategoryColor { get; set; } = "";
        public string VisibleUntilText { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsPinned { get; set; }
    }
}