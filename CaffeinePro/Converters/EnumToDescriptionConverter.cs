using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace CaffeinePro.Converters;

public class EnumToDescriptionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var fi = value?.GetType().GetField(value.ToString() ?? string.Empty);

        if (fi == null)
        {
            return value != null ? value.ToString() : string.Empty;
        }

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
