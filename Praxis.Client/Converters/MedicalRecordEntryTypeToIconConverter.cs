using System;
using System.Globalization;
using System.Windows.Data;
using Praxis.Domain.Constants;

namespace Praxis.Client.Converters;

public class MedicalRecordEntryTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not MedicalRecordEntryType type)
            return "•";

        return type switch
        {
            MedicalRecordEntryType.Anamnese => "📝",
            MedicalRecordEntryType.Befund => "🔎",
            MedicalRecordEntryType.Diagnose => "🏷",
            MedicalRecordEntryType.Therapie => "💊",
            MedicalRecordEntryType.Notiz => "📌",
            MedicalRecordEntryType.Labor => "🧪",
            MedicalRecordEntryType.Dokument => "📄",
            MedicalRecordEntryType.Abrechnung => "€",
            _ => "•"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}