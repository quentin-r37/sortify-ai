using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FileOrganizer.Converters
{
    public class NullableBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case bool boolValue:
                {
                    var invert = parameter != null && bool.TryParse(parameter.ToString(), out var isInverse) && isInverse;
                    return invert ? !boolValue : boolValue;
                }
                case null:
                    return false;
                default:
                    return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case bool boolValue:
                {
                    var invert = parameter != null && bool.TryParse(parameter.ToString(), out var isInverse) && isInverse;
                    return invert ? !boolValue : boolValue;
                }
                case null:
                    return null;
                default:
                    return false;
            }
        }
    }
}