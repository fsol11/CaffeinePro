﻿using System.Globalization;
using System.Windows.Data;

namespace CaffeinePro.Converters;

/// <summary>
/// Converts a string to a boolean value
/// </summary>
public class ValueEqualsParameterConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value == parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value is bool and true) ? parameter : Binding.DoNothing;
    }
}
