using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;

namespace Praxis.Client.Views.Pages.Dashboard
{
    public partial class DashboardPage
    {
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
            DragDrop.DoDragDrop(widget, dragData, System.Windows.DragDropEffects.Move);
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

    }
}
