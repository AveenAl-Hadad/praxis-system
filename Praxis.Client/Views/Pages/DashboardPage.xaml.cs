using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Praxis.Client.Views;
using Praxis.Domain.Entities;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using ColorConverter = System.Windows.Media.ColorConverter;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using ListBox = System.Windows.Controls.ListBox;




namespace Praxis.Client.Views.Pages
{

    public partial class DashboardPage : System.Windows.Controls.UserControl
    {
        private System.Windows.Point _kanbanDragStartPoint;
        private System.Windows.Point _widgetDragStartPoint;
        private FrameworkElement? _draggedWidget;
        private readonly int[] _widgetRows = { 2, 4, 6, 8, 10 };

        public DashboardPage()
        {
            InitializeComponent();
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
        private void ApplyWidgetOrder(List<string> order)
        {
            var widgetMap = new Dictionary<string, FrameworkElement>
            {
                ["Stats"] = StatsWidget,
                ["Overview"] = OverviewWidget,
                ["Tasks"] = TasksWidget,
                ["Notices"] = NoticesWidget,
                ["Appointments"] = AppointmentsWidget
            };

            var rowMap = new Dictionary<int, int>
            {
                [0] = 2,
                [1] = 4,
                [2] = 6,
                [3] = 8,
                [4] = 10
            };

            for (var i = 0; i < order.Count && i < 5; i++)
            {
                var key = order[i];
                if (widgetMap.TryGetValue(key, out var widget))
                {
                    Grid.SetRow(widget, rowMap[i]);
                }
            }
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
            try
            {
                if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                    return;

                var stats = await mainWindow.GetDashboardStatsAsync();

                var todayAppointments = (await mainWindow.GetAppointmentsByDateAsync(DateTime.Today))
                    .OrderBy(a => a.StartTime)
                    .ToList();

                var allTasks = (await mainWindow.GetAllDashboardTasksAsync())
                    .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                    .ThenByDescending(t => t.CreatedAt)
                    .ToList();

                var today = DateTime.Today;

                var completedTasks = allTasks
                    .Where(t => string.Equals(t.Status, "Erledigt", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var overdueTasks = allTasks
                    .Where(t => t.DueDate != null &&
                                t.DueDate.Value.Date < today &&
                                !string.Equals(t.Status, "Erledigt", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var dueTodayTasks = allTasks
                    .Where(t => t.DueDate != null &&
                                t.DueDate.Value.Date == today &&
                                !string.Equals(t.Status, "Erledigt", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var plainOpenTasks = allTasks
                    .Where(t =>
                        !string.Equals(t.Status, "Erledigt", StringComparison.OrdinalIgnoreCase) &&
                        !(t.DueDate != null && t.DueDate.Value.Date < today) &&
                        !(t.DueDate != null && t.DueDate.Value.Date == today))
                    .ToList();



                var activeNotices = (await mainWindow.GetActivePracticeNoticesAsync())
                    .ToList();

                // Gesamtzahlen
                TotalPatientsText.Text = stats.TotalPatients.ToString();
                TotalAppointmentsText.Text = stats.TotalAppointments.ToString();
                TotalInvoicesText.Text = stats.TotalInvoices.ToString();
                TotalPrescriptionsText.Text = stats.TotalPrescriptions.ToString();

                // Monatszahlen
                MonthAppointmentsText.Text = stats.CurrentMonthAppointments.ToString();
                MonthInvoicesText.Text = stats.CurrentMonthInvoices.ToString();
                MonthRevenueText.Text = $"{stats.CurrentMonthRevenue:N2} €";
                TotalRevenueText.Text = $"{stats.TotalRevenue:N2} €";

                // Heutige Kennzahlen
                TodayAppointmentsText.Text = todayAppointments.Count.ToString();
                PlannedAppointmentsText.Text = todayAppointments.Count(a =>
                    string.Equals(a.Status, "Geplant", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(a.Status)).ToString();

                CompletedAppointmentsText.Text = todayAppointments.Count(a =>
                    string.Equals(a.Status, "Erledigt", StringComparison.OrdinalIgnoreCase)).ToString();

                CancelledAppointmentsText.Text = todayAppointments.Count(a =>
                    string.Equals(a.Status, "Abgesagt", StringComparison.OrdinalIgnoreCase)).ToString();

                TodayDateText.Text = $"Stand: {DateTime.Now:dd.MM.yyyy HH:mm}";
                // Aufgaben-Kennzahlen
                OpenTasksText.Text = plainOpenTasks.Count.ToString();
                DueTodayTasksText.Text = dueTodayTasks.Count.ToString();
                OverdueTasksText.Text = overdueTasks.Count.ToString();
                //Kanban
                KanbanOpenCountText.Text = plainOpenTasks.Count.ToString();
                KanbanTodayCountText.Text = dueTodayTasks.Count.ToString();
                KanbanOverdueCountText.Text = overdueTasks.Count.ToString();
                KanbanCompletedCountText.Text = completedTasks.Count.ToString();

                KanbanOpenList.ItemsSource = plainOpenTasks
                    .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                    .Take(6)
                    .Select(MapKanbanCard)
                    .ToList();

                KanbanTodayList.ItemsSource = dueTodayTasks
                    .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                    .Take(6)
                    .Select(MapKanbanCard)
                    .ToList();

                KanbanOverdueList.ItemsSource = overdueTasks
                    .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                    .Take(6)
                    .Select(MapKanbanCard)
                    .ToList();

                KanbanCompletedList.ItemsSource = completedTasks
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(6)
                    .Select(MapKanbanCard)
                    .ToList();


                // Hinweise-Kennzahlen
                // Hinweise-Kennzahlen
                ActiveNoticesText.Text = activeNotices.Count.ToString();
                PinnedNoticesText.Text = activeNotices.Count(n => n.IsPinned).ToString();

                TodayAppointmentsGrid.ItemsSource = todayAppointments.Select(a => new DashboardAppointmentRow
                {
                    Time = a.StartTime.ToString("HH:mm"),
                    PatientName = a.Patient?.FullName ?? $"Patient #{a.PatientId}",
                    Reason = string.IsNullOrWhiteSpace(a.Reason) ? "-" : a.Reason,
                    Status = string.IsNullOrWhiteSpace(a.Status) ? "Geplant" : a.Status,
                    DurationMinutes = a.DurationMinutes
                }).ToList();

                TasksGrid.ItemsSource = allTasks.Select(t =>
                {
                    var priority = string.IsNullOrWhiteSpace(t.Priority) ? "Normal" : t.Priority;
                    var status = string.IsNullOrWhiteSpace(t.Status) ? "Offen" : t.Status;

                    var isCompleted = string.Equals(status, "Erledigt", StringComparison.OrdinalIgnoreCase);
                    var isDueToday =
                        t.DueDate != null &&
                        t.DueDate.Value.Date == DateTime.Today &&
                        !isCompleted;

                    var isOverdue =
                        t.DueDate != null &&
                        t.DueDate.Value.Date < DateTime.Today &&
                        !isCompleted;

                    return new DashboardTaskRow
                    {
                        Id = t.Id,
                        Title = string.IsNullOrWhiteSpace(t.Title) ? "-" : t.Title,
                        PatientName = t.Patient?.FullName ?? "-",
                        Priority = priority,
                        DueDate = t.DueDate?.ToString("dd.MM.yyyy") ?? "-",
                        Status = status,
                        AssignedTo = string.IsNullOrWhiteSpace(t.AssignedTo) ? "-" : t.AssignedTo,
                        PriorityColor = GetPriorityColor(priority),
                        StatusColor = GetTaskStatusColor(status),
                        IsCompleted = isCompleted,
                        IsDueToday = isDueToday,
                        IsOverdue = isOverdue,
                        DueDateColor = GetDueDateColor(isOverdue, isDueToday, isCompleted)
                    };
                }).ToList();

                var noticeRows = activeNotices.Select(n =>
                {
                    var category = string.IsNullOrWhiteSpace(n.Category) ? "Info" : n.Category;

                    return new PracticeNoticeRow
                    {
                        Id = n.Id,
                        Title = string.IsNullOrWhiteSpace(n.Title) ? "-" : n.Title,
                        Content = string.IsNullOrWhiteSpace(n.Content) ? "-" : n.Content,
                        Category = category,
                        CategoryColor = GetNoticeCategoryColor(category),
                        VisibleUntilText = n.VisibleUntil?.ToString("dd.MM.yyyy") ?? "-",
                        IsActive = n.IsActive,
                        IsPinned = n.IsPinned
                    };
                }).ToList();

                NoticesGrid.ItemsSource = noticeRows;
                NoticeCardsList.ItemsSource = noticeRows;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Laden des Dashboards:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshAsync();
        }
        private async void OpenPatientsButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            await mainWindow.OpenPatientSearchPageAsync();
        }
        private async void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            var dialog = new TaskEditWindow
            {
                Owner = Window.GetWindow(this)
            };

            var result = dialog.ShowDialog();
            if (result != true || dialog.ResultTask == null)
                return;

            try
            {
                await mainWindow.AddDashboardTaskAsync(dialog.ResultTask);
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Speichern der Aufgabe:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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
        private async void CompleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            if (TasksGrid.SelectedItem is not DashboardTaskRow selectedTask)
            {
                MessageBox.Show("Bitte zuerst eine Aufgabe auswählen.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Aufgabe '{selectedTask.Title}' als erledigt markieren?",
                "Aufgabe erledigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                await mainWindow.MarkDashboardTaskAsDoneAsync(selectedTask.Id);
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Abschließen der Aufgabe:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private async void TasksGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            if (TasksGrid.SelectedItem is not DashboardTaskRow selectedRow)
                return;

            try
            {
                var task = await mainWindow.GetDashboardTaskByIdAsync(selectedRow.Id);
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
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Bearbeiten der Aufgabe:\n{ex.Message}",
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
        private async void KanbanList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            if (sender is not System.Windows.Controls.ListBox listBox)
                return;

            if (listBox.SelectedItem is not KanbanTaskCardRow selectedCard)
                return;

            try
            {
                var task = await mainWindow.GetDashboardTaskByIdAsync(selectedCard.Id);
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
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Bearbeiten der Aufgabe:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void KanbanList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _kanbanDragStartPoint = e.GetPosition(null);
        }
        private void KanbanList_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var currentPosition = e.GetPosition(null);

            if (Math.Abs(currentPosition.X - _kanbanDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPosition.Y - _kanbanDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            if (sender is not System.Windows.Controls.ListBox listBox)
                return;

            if (listBox.SelectedItem is not KanbanTaskCardRow selectedCard)
                return;

            var dragData = new System.Windows.DataObject(typeof(KanbanTaskCardRow), selectedCard);

           DragDrop.DoDragDrop( listBox, dragData, System.Windows.DragDropEffects.Move);
        }
        private void KanbanList_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (sender is not ListBox listBox)
                return;

            if (e.Data.GetDataPresent(typeof(KanbanTaskCardRow)))
            {
                e.Effects = System.Windows.DragDropEffects.Move;
                ClearAllKanbanHighlights();
                SetKanbanDropHighlight(listBox, true);
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
                SetKanbanDropHighlight(listBox, false);
            }

            e.Handled = true;
        }
        private void KanbanList_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            if (sender is not ListBox listBox)
                return;

            SetKanbanDropHighlight(listBox, false);
            e.Handled = true;
        }
        private async void KanbanList_Drop(object sender, System.Windows.DragEventArgs e)
        {
            ClearAllKanbanHighlights();
            if (!e.Data.GetDataPresent(typeof(KanbanTaskCardRow)))
                return;

            if (sender is not ListBox targetListBox)
                return;

            if (targetListBox.Tag is not string targetLane)
                return;

            var draggedCard = e.Data.GetData(typeof(KanbanTaskCardRow)) as KanbanTaskCardRow;
            if (draggedCard == null)
                return;

            if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow)
                return;

            try
            {
                var task = await mainWindow.GetDashboardTaskByIdAsync(draggedCard.Id);
                if (task == null)
                {
                    MessageBox.Show("Die Aufgabe wurde nicht gefunden.");
                    return;
                }

                ApplyKanbanLaneToTask(task, targetLane);

                await mainWindow.UpdateDashboardTaskAsync(task);
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Verschieben der Aufgabe:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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
        private void KanbanList_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (sender is not ListBox listBox)
                return;

            if (!e.Data.GetDataPresent(typeof(KanbanTaskCardRow)))
                return;

            ClearAllKanbanHighlights();
            SetKanbanDropHighlight(listBox, true);

            e.Handled = true;
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

        private void Widget_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _widgetDragStartPoint = e.GetPosition(null);
        }

        private void Widget_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var currentPosition = e.GetPosition(null);

            if (Math.Abs(currentPosition.X - _widgetDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPosition.Y - _widgetDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            if (sender is not FrameworkElement widget)
                return;

            _draggedWidget = widget;

            var dragData = new System.Windows.DataObject(typeof(FrameworkElement), widget);
            System.Windows.DragDrop.DoDragDrop(widget, dragData, System.Windows.DragDropEffects.Move);
        }

        private void Widget_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (sender is not Border targetBorder)
                return;

            if (!e.Data.GetDataPresent(typeof(FrameworkElement)))
                return;

            SetWidgetDropHighlight(targetBorder, true);
            e.Handled = true;
        }

        private void Widget_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (sender is not Border targetBorder)
                return;

            if (e.Data.GetDataPresent(typeof(FrameworkElement)))
            {
                e.Effects = System.Windows.DragDropEffects.Move;
                ClearAllWidgetHighlights();
                SetWidgetDropHighlight(targetBorder, true);
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
                SetWidgetDropHighlight(targetBorder, false);
            }

            e.Handled = true;
        }

        private void Widget_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            if (sender is not Border targetBorder)
                return;

            SetWidgetDropHighlight(targetBorder, false);
            e.Handled = true;
        }

        private async void Widget_Drop(object sender, System.Windows.DragEventArgs e)
        {
            ClearAllWidgetHighlights();

            if (!e.Data.GetDataPresent(typeof(FrameworkElement)))
                return;

            if (sender is not FrameworkElement targetWidget)
                return;

            var sourceWidget = e.Data.GetData(typeof(FrameworkElement)) as FrameworkElement;
            if (sourceWidget == null || ReferenceEquals(sourceWidget, targetWidget))
                return;

            SwapWidgetRows(sourceWidget, targetWidget);

            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                var currentOrder = GetCurrentWidgetOrder();
                await mainWindow.SaveDashboardWidgetOrderAsync(currentOrder);
            }

            e.Handled = true;
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

        // Kontextmenü-Handler für das Grid Die Aufgaben bereich
        private async void EditTaskMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedTaskRow();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst eine Aufgabe auswählen.");
                return;
            }

            await EditTaskByIdAsync(selected.Id);
        }
        private async void CompleteTaskMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedTaskRow();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst eine Aufgabe auswählen.");
                return;
            }

            await CompleteTaskByIdAsync(selected.Id);
        }
        private async void ReopenTaskMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedTaskRow();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst eine Aufgabe auswählen.");
                return;
            }

            await ReopenTaskByIdAsync(selected.Id);
        }
        private async void DeleteTaskMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedTaskRow();
            if (selected == null)
            {
                MessageBox.Show("Bitte zuerst eine Aufgabe auswählen.");
                return;
            }

            await DeleteTaskByIdAsync(selected.Id);
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

        //Kontextmenü-Handler für Kanban-Karten
        private async void EditKanbanTaskMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetKanbanTaskFromContextMenu(sender);
            if (selected == null)
            {
                MessageBox.Show("Aufgabe konnte nicht erkannt werden.");
                return;
            }

            await EditTaskByIdAsync(selected.Id);
        }
        private async void CompleteKanbanTaskMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetKanbanTaskFromContextMenu(sender);
            if (selected == null)
            {
                MessageBox.Show("Aufgabe konnte nicht erkannt werden.");
                return;
            }

            await CompleteTaskByIdAsync(selected.Id);
        }
        private async void ReopenKanbanTaskMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetKanbanTaskFromContextMenu(sender);
            if (selected == null)
            {
                MessageBox.Show("Aufgabe konnte nicht erkannt werden.");
                return;
            }

            await ReopenTaskByIdAsync(selected.Id);
        }
        private async void DeleteKanbanTaskMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetKanbanTaskFromContextMenu(sender);
            if (selected == null)
            {
                MessageBox.Show("Aufgabe konnte nicht erkannt werden.");
                return;
            }

            await DeleteTaskByIdAsync(selected.Id);
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