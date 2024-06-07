using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CaffeinePro.Converters;

/// <summary>
/// Converts a string to a boolean value
/// </summary>
public class ValueNotEqualsParameterToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return false;
        }
        
        return (!value.Equals(parameter)) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value is Visibility and Visibility.Collapsed) ? parameter : Binding.DoNothing;
    }
}
