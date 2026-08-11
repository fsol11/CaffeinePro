using System.Globalization;
using System.Windows.Data;
using CaffeinePro.Classes;
using CaffeinePro.Controls;

namespace CaffeinePro.Converters;


/// <summary>
/// Converts minutes to a string
/// </summary>
public class MinutesToActualDateTimeStringConverter : IValueConverter, IMultiValueConverter
{


    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value == null)
        {
            return string.Empty;
        }

        // Defaults to including the date: a duration picked in the FOR section can land tomorrow, and
        // "1:56 PM" alone would hide that. The only place the date must be dropped is the startup
        // options, where the app is not running yet - and those callers pass the flag explicitly.
        var inStartupOptions = (parameter as bool?) ?? false;

        var minutes = 0;

        switch (value)
        {
            case string s:
                _ = int.TryParse(s, out minutes);
                break;
            case double d:
                minutes = (int)Math.Round(d);
                break;
            case decimal dec:
                minutes = (int) dec;
                break;
            case int i:
                minutes = i;
                break;
        }

        // A full day means an indefinite awakeness, which never lands on a clock time.
        if (minutes >= TimeSliderControl.IndefiniteMinutes)
        {
            return "Indefinitely";
        }

        var h = minutes / 60;
        var m = minutes % 60;


        return Routines.GetDateTimeString(Awakeness.GetNow().Add(new TimeSpan(h, m, 0)), !inStartupOptions);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }

    /// <summary>
    /// Multi-binding form of <see cref="Convert(object?,Type,object?,CultureInfo)"/>: the first value is
    /// the minutes and the second stands in for the converter parameter, which XAML cannot bind directly.
    /// </summary>
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        => Convert(
            values.Length > 0 ? values[0] : null,
            targetType,
            values.Length > 1 ? values[1] : parameter,
            culture);

    public object?[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
