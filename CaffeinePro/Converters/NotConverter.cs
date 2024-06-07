using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CaffeinePro.Converters;

    public class NotConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                Visibility visibility => visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed,
                bool b => !b,
                _ => throw new ArgumentException("Value must be of type Visibility or Boolean")
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                Visibility visibility => visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed,
                bool b => !b,
                _ => throw new ArgumentException("Value must be of type Visibility or Boolean")
            };
        }
    }
