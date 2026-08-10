using System.Globalization;
using System.Windows.Data;

namespace CaffeinePro.Converters;

/// <summary>
/// Converts a boolean to an opacity, dimming the element when true is passed. The dimmed opacity
/// defaults to 0.4 and can be overridden by passing it as the parameter.
/// </summary>
[ValueConversion(typeof(bool), typeof(double))]
public class BooleanToOpacityConverter : IValueConverter
{
    private const double DefaultDimmedOpacity = 0.4;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!true.Equals(value))
        {
            return 1.0;
        }

        return parameter is string text && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture,
            out var dimmed)
            ? dimmed
            : DefaultDimmedOpacity;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
