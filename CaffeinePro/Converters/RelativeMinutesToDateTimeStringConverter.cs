using System.Globalization;
using System.Windows.Data;
using CaffeinePro.Classes;

namespace CaffeinePro.Converters;


/// <summary>
/// Converts minutes to a string
/// </summary>
public class RelativeMinutesToDateTimeStringConverter : IValueConverter
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
        }

        var h = minutes / 60;
        var m = minutes % 60;


        return Routines.GetDateTimeString(DateTime.Now.Add(new TimeSpan(h, m, 0)));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
