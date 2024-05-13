using System.Globalization;
using System.Windows.Data;

namespace Caffeine_Pro.Converters;

[ValueConversion(typeof(double), typeof(int))]

public class DoubleToIntConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (value)
        {
            case null:
                return 0;
            case double doubleValue:
                return (int)Math.Round(doubleValue);
            default:
                try
                {
                    return (int)value;
                }
                catch
                {
                    return 0;
                }
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return 0.0;
        }

        try
        {
            return (double)value;
        }
        catch
        {
            return 0.0;
        }
    }
}