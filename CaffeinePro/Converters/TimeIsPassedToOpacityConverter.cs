using System.Globalization;
using System.Windows.Data;
using CaffeinePro.Classes;

namespace CaffeinePro.Converters;

/// <summary>
/// Converts a boolean to a visibility value. When true is passed, the value will be converted to Visibility.Visible.
/// The behavior can be inverted by passing the parameter "Inverted"
/// </summary>
public class TimeIsPassedToOpacityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var time = Routines.ContentToTimeSpan(values[0]);
        if (time == TimeSpan.MaxValue)
        {
            return 1.0;
        }
        
        if (values[1] is not DateTime datetime)
        {
            throw new ArgumentException($"The value must be a DateTime object: {values[1]}");
        }
        
        return datetime.TimeOfDay.Add(TimeSpan.FromMinutes(15)) < time ? 1.0 : 0.4;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[2];
    }

}