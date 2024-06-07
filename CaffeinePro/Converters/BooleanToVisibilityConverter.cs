using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CaffeinePro.Converters;

/// <summary>
/// Converts a boolean to a visibility value. When true is passed, the value will be converted to Visibility.Visible.
/// The behavior can be inverted by passing the parameter "Inverted"
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class BooleanToVisibilityConverter : IValueConverter
{
    public enum Parameter
    {
        Normal,
        Inverted,
        Invert
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var boolValue = true.Equals(value);

        var direction = parameter is string or Parameter
            ? (Parameter)Enum.Parse(typeof(Parameter), (string)parameter)
            : Parameter.Normal;
        if (direction is Parameter.Inverted or Parameter.Invert)
        {
            boolValue = !boolValue;
        }

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}