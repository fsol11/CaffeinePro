using System.Globalization;
using System.Windows.Data;
using CaffeinePro.Classes;

namespace CaffeinePro.Converters;

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

        var minutes = 0.0;

        switch (value)
        {
            case string s:
                _ = double.TryParse(s, out minutes);
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
            case TimeSpan at:
                minutes = at.TotalMinutes;
                break;
        }

        var h = minutes / 60;
        var m = minutes % 60;

        return Routines.GetTimeString(new TimeSpan((int) h, (int) m, 0), parameter is TimeStringFormat.Relative);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
