using System.Globalization;
using System.Windows.Data;

namespace Caffeine_Pro.Converters;

    public class NotConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !(bool)(value ?? true);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !(bool)(value ?? true);
        }
    }
