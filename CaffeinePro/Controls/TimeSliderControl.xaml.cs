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
                var ampm = startHour == 0 ? "AM" : "PM";
                return new HourItem(d.ToString(), ampm, []);
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

    private void QuarterButton_Click(object sender, RoutedEventArgs e)
    {
        var time = Routines.ContentToTimeSpan(sender);
        if (time != TimeSpan.MaxValue)
        {
            var dt = Routines.GetDateTimeFromTimeSpan(time, Awakeness.AwakenessTypes.Absolute);
            SetSliderValue(Routines.ToRelativeTime(dt).TotalMinutes, false);
        }
        e.Handled = true;
    }

    internal void SetRelativeTime(TimeSpan relativeSpan) => SetSliderValue(relativeSpan.TotalMinutes, false);

    private void HourButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Content?.ToString(), out var hour12))
        {
            return;
        }

        // The buttons are labelled 12, 1 ... 11 in both rows, so 12 AM is 0:00 and 12 PM is 12:00.
        var hour24 = (hour12 % 12) + ((button.Tag as string) == "PM" ? 12 : 0);

        var dt = Routines.GetDateTimeFromTimeSpan(TimeSpan.FromHours(hour24), Awakeness.AwakenessTypes.Absolute);
        SetSliderValue(Routines.ToRelativeTime(dt).TotalMinutes, false);
        e.Handled = true;
    }
}

public record QuarterItem(string Label, string Minutes);
public record HourItem(string HourLabel, string AmPm, IReadOnlyList<QuarterItem> Quarters);

