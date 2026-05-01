using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using ListBox = System.Windows.Controls.ListBox;

namespace Praxis.Client.Views.Pages.Dashboard
{
    public partial class DashboardPage
    {
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

            DragDrop.DoDragDrop(listBox, dragData, System.Windows.DragDropEffects.Move);
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
    }
}
