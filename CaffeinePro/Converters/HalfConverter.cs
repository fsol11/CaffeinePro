namespace CaffeinePro.Converters;

using System;
using System.Globalization;
using System.Windows.Data;

public class HalfConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return 0.0;
        }

        return (double)value / 2;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}