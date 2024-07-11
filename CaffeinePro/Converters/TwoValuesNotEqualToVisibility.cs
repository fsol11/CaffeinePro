using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CaffeinePro.Converters;

/// <summary>
/// Converts a string to a boolean value
/// </summary>
public class TwoValuesNotEqualToVisibility : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    { 
        if (values?[0] == null || values?[1] == null)
        {
            return Visibility.Collapsed;
        }

        return values[0].Equals(values[1]) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
