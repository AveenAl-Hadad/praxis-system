using System;
using System.Windows;
using System.Windows.Controls;
using Praxis.Domain.Entities;

namespace Praxis.Client.Views
{
    public partial class NoticeEditWindow : Window
    {
        public PracticeNotice? ResultNotice { get; private set; }

        public NoticeEditWindow()
        {
            InitializeComponent();
            VisibleUntilPicker.SelectedDate = DateTime.Today.AddDays(7);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var title = TitleTextBox.Text?.Trim() ?? string.Empty;
            var content = ContentTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(title))
            {
                System.Windows.MessageBox.Show("Bitte einen Titel eingeben.");
                return;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                System.Windows.MessageBox.Show("Bitte einen Hinweistext eingeben.");
                return;
            }

            var category = "Info";
            if (CategoryComboBox.SelectedItem is ComboBoxItem item &&
                item.Content is string selectedCategory &&
                !string.IsNullOrWhiteSpace(selectedCategory))
            {
                category = selectedCategory;
            }

            ResultNotice = new PracticeNotice
            {
                Title = title,
                Content = content,
                Category = category,
                IsPinned = PinnedCheckBox.IsChecked == true,
                IsActive = true,
                CreatedAt = DateTime.Now,
                VisibleUntil = VisibleUntilPicker.SelectedDate
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