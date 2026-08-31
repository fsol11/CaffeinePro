using System.Globalization;
using System.Windows.Data;
using CaffeinePro.Classes;
using CaffeinePro.Localization;

namespace CaffeinePro.Converters;

/// <summary>
/// Which part of an awakeness description <see cref="AwakenessToDisplayTextConverter"/> should return.
/// </summary>
public enum AwakenessTextPart
{
    /// <summary>The one line description: "Indefinitely", "For 2h : 30m" or "Until 05:00 PM".</summary>
    Summary,

    /// <summary>
    /// The clock time a relative awakeness lands on ("until 4:48 PM"). Empty for anything else,
    /// because those already say a clock time in the summary.
    /// </summary>
    AbsoluteHint
}

/// <summary>
/// Converts an <see cref="Awakeness"/> to a description, worded the same way the awakeness picker
/// words it.
/// </summary>
[ValueConversion(typeof(Awakeness), typeof(string))]
public class AwakenessToDisplayTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Awakeness awakeness)
        {
            return string.Empty;
        }

        if (parameter is AwakenessTextPart.AbsoluteHint)
        {
            // Counted from now rather than from the stored end date, which was calculated when the
            // settings were loaded and can be days old.
            return awakeness is { IsIndefinite: false, IsRelative: true }
                ? LocalizationService.Format("Awakeness_UntilHintFormat",
                    Routines.GetDateTimeString(Awakeness.GetNow().Add(awakeness.RelativeSpan)))
                : string.Empty;
        }

        if (awakeness.IsIndefinite)
        {
            return LocalizationService.Get("Common_Indefinitely");
        }

        if (awakeness.IsRelative)
        {
            var span = awakeness.RelativeSpan;
            return LocalizationService.Format("Awakeness_ForFormat",
                Routines.FormatDuration(span.Hours, span.Minutes));
        }

        // Absolute: only the time of day carries meaning. The stored end date can be days old, so it
        // is deliberately left out.
        return LocalizationService.Format("Awakeness_UntilFormat", awakeness.EndTimeText);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
