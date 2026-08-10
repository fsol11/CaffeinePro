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
        if (value == null)
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
                minutes = (int)dec;
                break;
            case int i:
                minutes = i;
                break;
            case TimeSpan at:
                minutes = at.TotalMinutes;
                break;
        }

        var h = (int) (minutes / 60);
        var m = (int) (minutes % 60);

        if (m == 0)
        {
            return h.ToString("0h");
        }
        else if (h < 1)
        {
            return m.ToString("0m");
        }
        else
        {
            return string.Format("{0}h : {1}m", h, m);
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
