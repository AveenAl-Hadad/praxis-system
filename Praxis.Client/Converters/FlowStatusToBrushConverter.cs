using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;

namespace Praxis.Client.Converters
{
    public class FlowStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var state = value?.ToString() ?? "";

            return state switch
            {
                "CheckedIn" => Brushes.White,
                "Waiting" => Brushes.LightBlue,
                "InTreatment" => Brushes.Khaki,
                "Completed" => Brushes.LightGray,
                _ => Brushes.White
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null!;
        }
    }
}