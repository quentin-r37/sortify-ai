using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FileOrganizer.Converters
{
    public class NullableBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Check if the parameter is set to invert the value.
                bool invert = parameter != null && bool.TryParse(parameter.ToString(), out var isInverse) && isInverse;
                
                return invert ? !boolValue : boolValue;
            }
            else if (value == null)
            {
                // Handle null case. You can return false or a different value based on the parameter.
                return false;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Check if the parameter is set to invert the value.
                bool invert = parameter != null && bool.TryParse(parameter.ToString(), out var isInverse) && isInverse;
                
                return invert ? !boolValue : boolValue;
            }
            else if (value == null)
            {
                // Handle null case.
                return null;
            }
            return false;
        }
    }
}