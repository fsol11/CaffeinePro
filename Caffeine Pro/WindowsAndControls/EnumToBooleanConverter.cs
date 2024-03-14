using System.Globalization;
using System.Windows.Data;

namespace Caffeine_Pro.WindowsAndControls;

public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return parameter != null && parameter.Equals(value);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null && value.Equals(true) ? parameter : Binding.DoNothing;
    }
}