using System.Windows;

using Praxis.Client.Views;
using Praxis.Domain.Entities;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using Praxis.Client.ViewModels;
using Praxis.Infrastructure.Services;

using ColorConverter = System.Windows.Media.ColorConverter;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.MessageBox;



namespace Praxis.Client.Views.Pages.Dashboard

{
    public partial class DashboardPage : System.Windows.Controls.UserControl
    {
        private System.Windows.Point _kanbanDragStartPoint;
        private System.Windows.Point _widgetDragStartPoint;
        private FrameworkElement? _draggedWidget;
        private readonly int[] _widgetRows = { 2, 4, 6, 8, 10 };

        public DashboardPage(DashboardViewModel dashboarsViewModel)
        {
            InitializeComponent();
            DataContext = dashboarsViewModel;

            Loaded += DashboardPage_Loaded;
        }
        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadWidgetLayoutAsync();
            await RefreshAsync();
        }
        private async Task LoadWidgetLayoutAsync()
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            var order = await mainWindow.GetDashboardWidgetOrderAsync();

            ApplyWidgetOrder(order);
        }
       
        private List<string> GetCurrentWidgetOrder()
        {
            var widgets = new List<(string Key, FrameworkElement Widget)>
                            {
                                ("Stats", StatsWidget),
                                ("Overview", OverviewWidget),
                                ("Tasks", TasksWidget),
                                ("Notices", NoticesWidget),
                                ("Appointments", AppointmentsWidget)
                            };

            return widgets
                .OrderBy(w => Grid.GetRow(w.Widget))
                .Select(w => w.Key)
                .ToList();
        }
        private async void ResetLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            var defaultOrder = new List<string>
    {
        "Stats",
        "Overview",
        "Tasks",
        "Notices",
        "Appointments"
    };

            ApplyWidgetOrder(defaultOrder);

            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                await mainWindow.SaveDashboardWidgetOrderAsync(defaultOrder);
            }
        }

        public async Task RefreshAsync()
        {
            if (DataContext is DashboardViewModel viewModel)
            {
                await viewModel.RefreshAsync();
            }
        }
        private async void OpenPatientsButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            await mainWindow.OpenPatientSearchPageAsync();
        }      
        private async void AddNoticeButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            var dialog = new NoticeEditWindow
            {
                Owner = Window.GetWindow(this)
            };

            var result = dialog.ShowDialog();
            if (result != true || dialog.ResultNotice == null)
                return;

            try
            {
                await mainWindow.AddPracticeNoticeAsync(dialog.ResultNotice);
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Speichern des Hinweises:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private async void DeactivateNoticeButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            if (NoticesGrid.SelectedItem is not PracticeNoticeRow selectedNotice)
            {
                MessageBox.Show("Bitte zuerst einen Hinweis auswählen.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Hinweis '{selectedNotice.Title}' deaktivieren?",
                "Hinweis deaktivieren",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                await mainWindow.DeactivatePracticeNoticeAsync(selectedNotice.Id);
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Deaktivieren des Hinweises:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
       
        private async void NoticesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            if (NoticesGrid.SelectedItem is not PracticeNoticeRow selectedRow)
                return;

            try
            {
                var notices = await mainWindow.GetActivePracticeNoticesAsync();
                var notice = notices.FirstOrDefault(n => n.Id == selectedRow.Id);

                if (notice == null)
                {
                    MessageBox.Show("Der Hinweis wurde nicht gefunden.");
                    return;
                }

                var dialog = new NoticeEditWindow(notice)
                {
                    Owner = Window.GetWindow(this)
                };

                var result = dialog.ShowDialog();
                if (result != true || dialog.ResultNotice == null)
                    return;

                await mainWindow.UpdatePracticeNoticeAsync(dialog.ResultNotice);
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Bearbeiten des Hinweises:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private static string GetPriorityColor(string? priority)
        {
            return (priority ?? string.Empty).Trim().ToLower() switch
            {
                "hoch" => "#DC2626",      // rot
                "normal" => "#D97706",    // orange
                "niedrig" => "#16A34A",   // grün
                _ => "#6B7280"            // grau
            };
        }
        private static string GetTaskStatusColor(string? status)
        {
            return (status ?? string.Empty).Trim().ToLower() switch
            {
                "offen" => "#2563EB",          // blau
                "inbearbeitung" => "#D97706",  // orange
                "erledigt" => "#6B7280",       // grau
                _ => "#6B7280"
            };
        }
        private static string GetNoticeCategoryColor(string? category)
        {
            return (category ?? string.Empty).Trim().ToLower() switch
            {
                "warnung" => "#DC2626",   // rot
                "wichtig" => "#D97706",   // orange
                "info" => "#2563EB",      // blau
                _ => "#6B7280"
            };
        }
        private static string GetDueDateColor(bool isOverdue, bool isDueToday, bool isCompleted)
        {
            if (isCompleted)
                return "#9CA3AF"; // grau

            if (isOverdue)
                return "#DC2626"; // rot

            if (isDueToday)
                return "#D97706"; // orange

            return "#374151"; // normal
        }
        private static KanbanTaskCardRow MapKanbanCard(DashboardTask task)
        {
            var patient = task.Patient?.FullName ?? "Ohne Patient";
            var due = task.DueDate?.ToString("dd.MM.yyyy") ?? "Kein Datum";
            var assigned = string.IsNullOrWhiteSpace(task.AssignedTo) ? "Nicht zugewiesen" : task.AssignedTo;

            return new KanbanTaskCardRow
            {
                Id = task.Id,
                Title = string.IsNullOrWhiteSpace(task.Title) ? "-" : task.Title,
                Subtitle = $"{patient} • {due} • {assigned}"
            };
        }
        
        private static void ApplyKanbanLaneToTask(DashboardTask task, string targetLane)
        {
            var today = DateTime.Today;

            switch (targetLane)
            {
                case "Open":
                    task.Status = "Offen";

                    if (task.DueDate != null && task.DueDate.Value.Date <= today)
                    {
                        task.DueDate = today.AddDays(1);
                    }
                    break;

                case "Today":
                    task.Status = "Offen";
                    task.DueDate = today;
                    break;

                case "Overdue":
                    task.Status = "Offen";

                    if (task.DueDate == null || task.DueDate.Value.Date >= today)
                    {
                        task.DueDate = today.AddDays(-1);
                    }
                    break;

                case "Completed":
                    task.Status = "Erledigt";
                    break;
            }
        }
        private static void SetKanbanDropHighlight(ListBox listBox, bool isActive)
        {
            if (!isActive)
            {
                listBox.Background = Brushes.Transparent;
                listBox.BorderThickness = new Thickness(0);
                return;
            }

            var lane = listBox.Tag?.ToString() ?? string.Empty;

            switch (lane)
            {
                case "Open":
                    listBox.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE"));
                    listBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60A5FA"));
                    listBox.BorderThickness = new Thickness(2);
                    break;

                case "Today":
                    listBox.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                    listBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                    listBox.BorderThickness = new Thickness(2);
                    break;

                case "Overdue":
                    listBox.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
                    listBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                    listBox.BorderThickness = new Thickness(2);
                    break;

                case "Completed":
                    listBox.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
                    listBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF"));
                    listBox.BorderThickness = new Thickness(2);
                    break;

                default:
                    listBox.Background = Brushes.Transparent;
                    listBox.BorderThickness = new Thickness(0);
                    break;
            }
        }
       
        private void ClearAllKanbanHighlights()
        {
            SetKanbanDropHighlight(KanbanOpenList, false);
            SetKanbanDropHighlight(KanbanTodayList, false);
            SetKanbanDropHighlight(KanbanOverdueList, false);
            SetKanbanDropHighlight(KanbanCompletedList, false);
        }

        //Hilfsmethoden
        // Augaben aus Grid holen
        private DashboardTaskRow? GetSelectedTaskRow()
        {
            return TasksGrid.SelectedItem as DashboardTaskRow;
        }
        private PracticeNoticeRow? GetSelectedNoticeRow()
        {
            return NoticesGrid.SelectedItem as PracticeNoticeRow;
        }
        //Aufgabe aus Kanban-Kontextmenü holen
        private KanbanTaskCardRow? GetKanbanTaskFromContextMenu(object sender)
        {
            if (sender is not FrameworkElement element)
                return null;

            if (element.DataContext is KanbanTaskCardRow directCard)
                return directCard;

            return null;
        }
        // Zentrale Bearbeiten-Methode
        private async Task EditTaskByIdAsync(int taskId)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            var task = await mainWindow.GetDashboardTaskByIdAsync(taskId);
            if (task == null)
            {
                MessageBox.Show("Die Aufgabe wurde nicht gefunden.");
                return;
            }

            var dialog = new TaskEditWindow(task)
            {
                Owner = Window.GetWindow(this)
            };

            var result = dialog.ShowDialog();
            if (result != true || dialog.ResultTask == null)
                return;

            await mainWindow.UpdateDashboardTaskAsync(dialog.ResultTask);
            await RefreshAsync();
        }
        //Erleding
        private async Task CompleteTaskByIdAsync(int taskId)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            await mainWindow.MarkDashboardTaskAsDoneAsync(taskId);
            await RefreshAsync();
        }
        //Offen
        private async Task ReopenTaskByIdAsync(int taskId)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            await mainWindow.MoveDashboardTaskToOpenAsync(taskId);
            await RefreshAsync();
        }
        //Löschen
        private async Task DeleteTaskByIdAsync(int taskId)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            var confirm = MessageBox.Show(
                "Aufgabe wirklich löschen?",
                "Löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            await mainWindow.DeleteDashboardTaskAsync(taskId);
            await RefreshAsync();
        }

        // Hinweis Bereich
        private async Task EditNoticeByIdAsync(int noticeId)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            var notices = await mainWindow.GetActivePracticeNoticesAsync();
            var notice = notices.FirstOrDefault(n => n.Id == noticeId);

            if (notice == null)
            {
                MessageBox.Show("Hinweis wurde nicht gefunden.");
                return;
            }

            var dialog = new NoticeEditWindow(notice)
            {
                Owner = Window.GetWindow(this)
            };

            var result = dialog.ShowDialog();
            if (result != true || dialog.ResultNotice == null)
                return;

            await mainWindow.UpdatePracticeNoticeAsync(dialog.ResultNotice);
            await RefreshAsync();
        }
        private async Task SetNoticeActiveStateAsync(int noticeId, bool isActive)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            var notices = await mainWindow.GetActivePracticeNoticesAsync();
            var notice = notices.FirstOrDefault(n => n.Id == noticeId);

            if (notice == null)
            {
                MessageBox.Show("Hinweis wurde nicht gefunden.");
                return;
            }

            notice.IsActive = isActive;
            await mainWindow.UpdatePracticeNoticeAsync(notice);
            await RefreshAsync();
        }
        private async Task DeleteNoticeByIdAsync(int noticeId)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            var confirm = MessageBox.Show(
                "Hinweis wirklich löschen?",
                "Löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            await mainWindow.DeletePracticeNoticeAsync(noticeId);
            await RefreshAsync();
        }
            
        private static void SwapWidgetRows(FrameworkElement first, FrameworkElement second)
        {
            var firstRow = Grid.GetRow(first);
            var secondRow = Grid.GetRow(second);

            Grid.SetRow(first, secondRow);
            Grid.SetRow(second, firstRow);
        }
        private void SetWidgetDropHighlight(Border border, bool isActive)
        {
            if (!isActive)
            {
                border.BorderBrush = null;
                border.BorderThickness = new Thickness(0);
                return;
            }

            border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60A5FA"));
            border.BorderThickness = new Thickness(2);
        }
        private void ClearAllWidgetHighlights()
        {
            SetWidgetDropHighlight(StatsWidget, false);
            SetWidgetDropHighlight(OverviewWidget, false);
            SetWidgetDropHighlight(TasksWidget, false);
            SetWidgetDropHighlight(NoticesWidget, false);
            SetWidgetDropHighlight(AppointmentsWidget, false);
        }

        
        // Hinweis Bersich
        private async void EditNoticeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedNoticeRow();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst einen Hinweis auswählen.");
                return;
            }

            await EditNoticeByIdAsync(selected.Id);
        }
        private async void DeactivateNoticeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedNoticeRow();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst einen Hinweis auswählen.");
                return;
            }

            await SetNoticeActiveStateAsync(selected.Id, false);
        }
        private async void ActivateNoticeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedNoticeRow();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst einen Hinweis auswählen.");
                return;
            }

            await SetNoticeActiveStateAsync(selected.Id, true);
        }
        private async void DeleteNoticeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedNoticeRow();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst einen Hinweis auswählen.");
                return;
            }

            await DeleteNoticeByIdAsync(selected.Id);
        }     

        //Kontextmenü-Handler für Kanban-Karten Hinweis Bereich
        private async void EditNoticeCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not PracticeNoticeRow selected)
            {
                MessageBox.Show("Hinweis konnte nicht erkannt werden.");
                return;
            }

            await EditNoticeByIdAsync(selected.Id);
        }
        private async void DeactivateNoticeCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not PracticeNoticeRow selected)
            {
                MessageBox.Show("Hinweis konnte nicht erkannt werden.");
                return;
            }

            await SetNoticeActiveStateAsync(selected.Id, false);
        }
        private async void DeleteNoticeCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not PracticeNoticeRow selected)
            {
                MessageBox.Show("Hinweis konnte nicht erkannt werden.");
                return;
            }

            await DeleteNoticeByIdAsync(selected.Id);
        }

        private sealed class DashboardAppointmentRow
        {
            public string Time { get; set; } = string.Empty;
            public string PatientName { get; set; } = string.Empty;
            public string Reason { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int DurationMinutes { get; set; }
        }
        private sealed class DashboardTaskRow
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string PatientName { get; set; } = string.Empty;
            public string Priority { get; set; } = string.Empty;
            public string DueDate { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string AssignedTo { get; set; } = string.Empty;

            public string PriorityColor { get; set; } = "#6B7280";
            public string StatusColor { get; set; } = "#6B7280";
            public string DueDateColor { get; set; } = "#374151"; // default dunkelgrau

            public bool IsOverdue { get; set; }
            public bool IsDueToday { get; set; }
            public bool IsCompleted { get; set; }
        }       
        private sealed class KanbanTaskCardRow
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Subtitle { get; set; } = string.Empty;
        }
        private sealed class PracticeNoticeRow
        {
            public int Id { get; set; }

            public string Title { get; set; } = string.Empty;

            public string Content { get; set; } = string.Empty;

            public string Category { get; set; } = string.Empty;

            public string CategoryColor { get; set; } = "#6B7280";

            public string VisibleUntilText { get; set; } = "-";

            public bool IsActive { get; set; }

            public bool IsPinned { get; set; }
        }
    }

}