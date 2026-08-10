using System.Globalization;
using System.Windows.Data;

namespace CaffeinePro.Converters;

/// <summary>
/// Converts hours and minutes to a string, omitting the minutes part when it is zero
/// </summary>
public class HoursMinutesToStringConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is not [int hours, int minutes])
        {
            return string.Empty;
        }

        return minutes == 0 ? $"{hours}h" : $"{hours}h : {minutes}m";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
