using System.Globalization;
using System.Windows.Data;

namespace Caffeine_Pro.Converters;

/// <summary>
/// Converts a string to a boolean value
/// </summary>
public class ValueNotEqualsParameterConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return false;
        }
        
        return (!value.Equals(parameter));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value is bool and false) ? parameter : Binding.DoNothing;
    }
}
