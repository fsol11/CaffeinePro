using System.Globalization;
using System.Windows.Data;

namespace TimePicker;

public class DigitalTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DigitalTime time)
        {
            return time.ToTimeSpan().Ticks;
        }

        throw new NotSupportedException();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            long l => new DigitalTime(TimeSpan.FromTicks(l)),
            int i => new DigitalTime(TimeSpan.FromTicks(i)),
            double d => new DigitalTime(TimeSpan.FromTicks((long)d)),
            _ => throw new NotSupportedException()
        };
    }
}
