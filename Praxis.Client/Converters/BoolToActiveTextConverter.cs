using System;
using System.Globalization;
using System.Windows.Data;

namespace Praxis.Client.Converters;

public class BoolToActiveTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is bool b && b) ? "Aktiv" : "Inaktiv";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string s && s.Equals("Aktiv", StringComparison.OrdinalIgnoreCase);
}