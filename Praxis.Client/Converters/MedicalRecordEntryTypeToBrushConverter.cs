using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Praxis.Domain.Constants;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace Praxis.Client.Converters;

public class MedicalRecordEntryTypeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not MedicalRecordEntryType type)
            return Brushes.White;

        return type switch
        {
            MedicalRecordEntryType.Anamnese => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE")),
            MedicalRecordEntryType.Befund => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7")),
            MedicalRecordEntryType.Diagnose => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7")),
            MedicalRecordEntryType.Therapie => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EDE9FE")),
            MedicalRecordEntryType.Notiz => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6")),
            MedicalRecordEntryType.Labor => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCFBF1")),
            MedicalRecordEntryType.Dokument => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E7FF")),
            MedicalRecordEntryType.Abrechnung => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2")),
            _ => Brushes.White
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}