using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Praxis.Domain.Entities;

namespace Praxis.Client.Views
{
    public partial class NoticeEditWindow : Window
    {
        private readonly int? _noticeId;
        private readonly DateTime _createdAt;
        private readonly bool _isActive;

        public PracticeNotice? ResultNotice { get; private set; }

        public NoticeEditWindow()
        {
            InitializeComponent();

            _noticeId = null;
            _createdAt = DateTime.Now;
            _isActive = true;

            VisibleUntilPicker.SelectedDate = DateTime.Today.AddDays(7);
            CategoryComboBox.SelectedIndex = 0;
        }

        public NoticeEditWindow(PracticeNotice notice)
        {
            InitializeComponent();

            _noticeId = notice.Id;
            _createdAt = notice.CreatedAt;
            _isActive = notice.IsActive;

            TitleTextBox.Text = notice.Title;
            ContentTextBox.Text = notice.Content;
            VisibleUntilPicker.SelectedDate = notice.VisibleUntil;
            PinnedCheckBox.IsChecked = notice.IsPinned;

            var category = string.IsNullOrWhiteSpace(notice.Category) ? "Info" : notice.Category;

            foreach (var item in CategoryComboBox.Items.OfType<ComboBoxItem>())
            {
                if (string.Equals(item.Content?.ToString(), category, StringComparison.OrdinalIgnoreCase))
                {
                    CategoryComboBox.SelectedItem = item;
                    break;
                }
            }
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
                Id = _noticeId ?? 0,
                CreatedAt = _createdAt,
                IsActive = _isActive,
                Title = title,
                Content = content,
                Category = category,
                IsPinned = PinnedCheckBox.IsChecked == true,
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