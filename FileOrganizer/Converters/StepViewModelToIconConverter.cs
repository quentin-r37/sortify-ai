using Avalonia.Data.Converters;
using FileOrganizer.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace FileOrganizer.Converters;

public class StepViewModelToIconConverter : IMultiValueConverter
{
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is not [bool completed, StepViewModel viewModel]) return "";
        return completed ? "fa-check" : viewModel.Icon;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}