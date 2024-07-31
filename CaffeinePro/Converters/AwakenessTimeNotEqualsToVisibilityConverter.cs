using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CaffeinePro.Classes;

namespace CaffeinePro.Converters;

/// <summary>
/// Converts a string to a boolean value
/// </summary>
[ValueConversion(typeof(Awakeness), typeof(Visibility))]
public class AwakenessTimeNotEqualsToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 && values[0] is Awakeness a1 && values[1] is Awakeness a2)
        {
            return a1.EqualsExceptDate(a2) ? Visibility.Collapsed : Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) 
        => throw new NotImplementedException();

}
