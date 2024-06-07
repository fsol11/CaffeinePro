using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CaffeinePro.Converters;

/// <summary>
/// Converts a date to a visibility value. When DateTime.MaxValue is passed,
/// the value will be converted to Visibility.Collapsed
/// </summary>
[ValueConversion(typeof(DateTime), typeof(Visibility))]
public class DateToEnabledConverter : IValueConverter
{
    public enum Parameter
    {
        Normal, Inverted
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var boolValue = !DateTime.MaxValue.Equals(value);

        var direction = parameter is string or Parameter ? (Parameter)Enum.Parse(typeof(Parameter), (string)parameter) : Parameter.Normal;
        if (direction == Parameter.Inverted) boolValue = !boolValue;
        return boolValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
