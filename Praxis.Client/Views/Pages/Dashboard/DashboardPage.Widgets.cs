using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace Praxis.Client.Views.Pages.Dashboard
{
    public partial class DashboardPage
    {
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


    }
}
