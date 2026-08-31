using System.Globalization;
using System.Windows.Data;
using CaffeinePro.Classes;

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

        return Routines.FormatDuration(hours, minutes);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
