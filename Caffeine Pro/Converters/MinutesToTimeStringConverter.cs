using System.Globalization;
using System.Windows.Data;
using Caffeine_Pro.Classes;

namespace Caffeine_Pro.Converters;

public enum TimeStringFormat
{
    Absolute,
    Relative
}

/// <summary>
/// Converts minutes to a string
/// </summary>
public class MinutesToTimeStringConverter : IValueConverter
{


    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value == null)
        {
            return string.Empty;
        }

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
            case AnalogTime at:
                minutes = at.TotalMinutes;
                break;
        }

        var h = minutes / 60;
        var m = minutes % 60;

        return Routines.GetTimeString(new TimeSpan(h, m, 0), parameter is TimeStringFormat.Relative);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
