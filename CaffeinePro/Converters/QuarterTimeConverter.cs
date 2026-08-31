using System.Globalization;
using System.Windows.Data;
using CaffeinePro.Classes;
using CaffeinePro.Controls;

namespace CaffeinePro.Converters;

/// <summary>
/// Builds the label of one entry in an hour's quarter-hour drop-down - "6:15 PM" - from the
/// <see cref="HourItem"/> it belongs to and a converter parameter holding the minutes.
/// </summary>
/// <remarks>
/// Assembled here rather than from separate runs in the XAML for two reasons. The AM/PM marker is
/// translated, which a hard-coded run in the markup could never be. And the pieces have to travel
/// as one string: split across runs, the bidirectional algorithm reorders each one on its own and
/// the time comes out scrambled ("PM 00: 6"), whereas a single string is laid out as a unit - the
/// digits keep their order, and the marker lands on whichever side the language reads it from.
/// </remarks>
[ValueConversion(typeof(HourItem), typeof(string))]
public class QuarterTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not HourItem hour
            || !int.TryParse(parameter as string, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out var minutes))
        {
            return string.Empty;
        }

        // The same helper every other clock time in the app goes through, so the drop-down cannot
        // drift out of step with the summary underneath the slider.
        return Routines.FormatClockTime(hour.Hour24, minutes);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
