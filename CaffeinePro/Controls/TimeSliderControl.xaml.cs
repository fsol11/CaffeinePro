using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CaffeinePro.Classes;

namespace CaffeinePro.Controls;

/// <summary>
/// Interaction logic for TimePickerSlider.xaml
/// </summary>
public partial class TimeSliderControl
{
    public static IReadOnlyList<HourItem> AmHours { get; } = GenerateHours(0);
    public static IReadOnlyList<HourItem> PmHours { get; } = GenerateHours(12);

    private static HourItem[] GenerateHours(int startHour) =>
        Enumerable.Range(startHour, 12)
            .Select(h =>
            {
                var d = h % 12 == 0 ? 12 : h % 12; // <- 0 and 12 are both displayed as "12"
                return new HourItem(d.ToString(), h);
            })
            .ToArray();

    public static string[] Minutes1 => ["15", "30", "45", "1440"];

    public static string?[] Minutes2 { get; } = GenerateMinutes2();

    private static string?[] GenerateMinutes2() =>
        [
            .. Enumerable.Range(2, 23)
                .Select(i => (string?)(i * 30).ToString())
,
            "1439",
        ];

    /// <summary>
    /// The top of the slider's range. A full day is not offered as a 24 hour duration: it stands for
    /// an indefinite awakeness, which is also where an indefinite awakeness lands when the slider is
    /// seeded from one.
    /// </summary>
    public const double IndefiniteMinutes = 1440;

    public TimeSpan TotalMinutes => TimeSpan.FromMinutes(MinutesSlider.Value);

    public static readonly DependencyProperty InStartupOptionsProperty = DependencyProperty.Register(nameof(InStartupOptions), typeof(bool), typeof(TimeSliderControl), new PropertyMetadata(default(bool)));

    public static readonly DependencyProperty IsForSelectionProperty = DependencyProperty.Register(nameof(IsForSelection), typeof(bool), typeof(TimeSliderControl), new PropertyMetadata(default(bool)));

    public static readonly DependencyProperty IsIndefiniteProperty = DependencyProperty.Register(nameof(IsIndefinite), typeof(bool), typeof(TimeSliderControl), new PropertyMetadata(default(bool)));

    private bool _isProgrammaticSliderChange;

    public TimeSliderControl()
    {
        InitializeComponent();
    }

    public bool InStartupOptions
    {
        get => (bool)GetValue(InStartupOptionsProperty);
        set => SetValue(InStartupOptionsProperty, value);
    }

    /// <summary>
    /// True when the current slider value was picked from the FOR section (quick picks or the slider itself),
    /// false when it was picked from the UNTIL section (absolute hour/quarter buttons).
    /// </summary>
    public bool IsForSelection
    {
        get => (bool)GetValue(IsForSelectionProperty);
        set => SetValue(IsForSelectionProperty, value);
    }

    /// <summary>
    /// True when the slider sits at the top of its range, which means "indefinitely" rather than
    /// "for 24 hours".
    /// </summary>
    public bool IsIndefinite
    {
        get => (bool)GetValue(IsIndefiniteProperty);
        private set => SetValue(IsIndefiniteProperty, value);
    }

    private void SetSliderValue(double minutes, bool isForSelection)
    {
        _isProgrammaticSliderChange = true;
        // An indefinite awakeness maps to TimeSpan.MaxValue, which is far outside the slider range.
        MinutesSlider.Value = Math.Clamp(Math.Round(minutes), MinutesSlider.Minimum, MinutesSlider.Maximum);
        _isProgrammaticSliderChange = false;
        IsForSelection = isForSelection;
    }

    private void MinutesSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Updated for programmatic changes as well: seeding the slider from an indefinite awakeness
        // clamps to the maximum, and the display has to follow.
        IsIndefinite = e.NewValue >= IndefiniteMinutes;

        if (_isProgrammaticSliderChange)
        {
            return;
        }

        IsForSelection = true;
    }

    private void MenuItemOnClick_ActiveFor(object sender, RoutedEventArgs e)
    {
        var menu = (MenuItem)sender;
        if (menu.Tag == null)
        {
            return;
        }

        SetSliderValue(int.Parse((menu.Tag as string)!), true);
    }

    /// <summary>
    /// One of the quarter-hour entries in an hour's drop-down. The hour comes from the
    /// <see cref="HourItem"/> the button inherits, the minutes from its Tag.
    /// </summary>
    /// <remarks>
    /// Read from the data rather than from the text on the button. The label is a translated,
    /// left-to-right clock time whose digits may not even be 0-9, so parsing it back would be both
    /// fragile and pointless when the values it encodes are right here.
    /// </remarks>
    private void QuarterButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;

        if (sender is not Button { DataContext: HourItem hour, Tag: string minutesTag }
            || !int.TryParse(minutesTag, out var minutes))
        {
            return;
        }

        SelectAbsoluteTime(new TimeSpan(hour.Hour24, minutes, 0));
    }

    /// <summary>
    /// Moves the slider to the picked clock time, counted forward from now.
    /// </summary>
    private void SelectAbsoluteTime(TimeSpan timeOfDay)
    {
        var dt = Routines.GetDateTimeFromTimeSpan(timeOfDay, Awakeness.AwakenessTypes.Absolute);
        SetSliderValue(Routines.ToRelativeTime(dt).TotalMinutes, false);
    }

    internal void SetRelativeTime(TimeSpan relativeSpan) => SetSliderValue(relativeSpan.TotalMinutes, false);

    private void HourButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;

        if (sender is Button { DataContext: HourItem hour })
        {
            SelectAbsoluteTime(TimeSpan.FromHours(hour.Hour24));
        }
    }
}

/// <summary>
/// One hour button in the UNTIL grid, and the drop-down of quarter hours behind it.
/// </summary>
/// <param name="HourLabel">
/// The hour as the button shows it - 12, 1 ... 11 in both rows. Always 0-9, whatever the language:
/// it is only ever text, and WPF draws it in the language's own digits.
/// </param>
/// <param name="Hour24">
/// The hour this actually means, on a 24 hour clock. Carried alongside the label so that picking a
/// time never depends on reading the label back.
/// </param>
public record HourItem(string HourLabel, int Hour24);

