using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using FileOrganizer.ViewModels;

namespace FileOrganizer.Converters
{
   public class StepViewModelToIconConverter : IMultiValueConverter
{
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is not [bool completed, StepViewModel viewModel])
        {
            return "";
        }
        return completed ? "fa-check" : viewModel.Icon;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
}
