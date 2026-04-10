using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Praxis.Domain.Entities;

namespace Praxis.Client.Views
{
    public partial class TaskEditWindow : Window
    {
        private readonly int? _taskId;
        private readonly int? _patientId;
        private readonly DateTime _createdAt;
        private readonly string _status;

        public DashboardTask? ResultTask { get; private set; }

        public TaskEditWindow()
        {
            InitializeComponent();

            _taskId = null;
            _patientId = null;
            _createdAt = DateTime.Now;
            _status = "Offen";

            DueDatePicker.SelectedDate = DateTime.Today;
            PriorityComboBox.SelectedIndex = 1;
        }

        public TaskEditWindow(DashboardTask task)
        {
            InitializeComponent();

            _taskId = task.Id;
            _patientId = task.PatientId;
            _createdAt = task.CreatedAt;
            _status = string.IsNullOrWhiteSpace(task.Status) ? "Offen" : task.Status;

            TitleTextBox.Text = task.Title;
            DescriptionTextBox.Text = task.Description;
            AssignedToTextBox.Text = task.AssignedTo;
            DueDatePicker.SelectedDate = task.DueDate;

            var priority = string.IsNullOrWhiteSpace(task.Priority) ? "Normal" : task.Priority;

            foreach (var item in PriorityComboBox.Items.OfType<ComboBoxItem>())
            {
                if (string.Equals(item.Content?.ToString(), priority, StringComparison.OrdinalIgnoreCase))
                {
                    PriorityComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var title = TitleTextBox.Text?.Trim() ?? string.Empty;
            var description = DescriptionTextBox.Text?.Trim() ?? string.Empty;
            var assignedTo = AssignedToTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(title))
            {
                System.Windows.MessageBox.Show("Bitte einen Titel eingeben.");
                return;
            }

            var priority = "Normal";
            if (PriorityComboBox.SelectedItem is ComboBoxItem item &&
                item.Content is string content &&
                !string.IsNullOrWhiteSpace(content))
            {
                priority = content;
            }

            ResultTask = new DashboardTask
            {
                Id = _taskId ?? 0,
                PatientId = _patientId,
                CreatedAt = _createdAt,
                Status = _status,
                Title = title,
                Description = description,
                Priority = priority,
                DueDate = DueDatePicker.SelectedDate,
                AssignedTo = assignedTo
            };

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}