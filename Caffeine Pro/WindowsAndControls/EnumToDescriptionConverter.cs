using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace Caffeine_Pro.WindowsAndControls;

public class EnumToDescriptionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var fi = value?.GetType().GetField(value.ToString() ?? string.Empty);

        if (fi == null) return string.Empty;

        var attributes =
            (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute), false);

        return attributes.Length > 0 ? attributes[0].Description : value?.ToString();

    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
