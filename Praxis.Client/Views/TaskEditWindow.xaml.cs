using System;
using System.Windows;
using System.Windows.Controls;
using Praxis.Domain.Entities;

namespace Praxis.Client.Views
{
    public partial class TaskEditWindow : Window
    {
        public DashboardTask? ResultTask { get; private set; }

        public TaskEditWindow()
        {
            InitializeComponent();
            DueDatePicker.SelectedDate = DateTime.Today;
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
                Title = title,
                Description = description,
                Priority = priority,
                DueDate = DueDatePicker.SelectedDate,
                AssignedTo = assignedTo,
                Status = "Offen",
                CreatedAt = DateTime.Now
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