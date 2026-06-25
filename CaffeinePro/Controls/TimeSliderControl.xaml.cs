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
                var d = (h % 12 == 0 && startHour > 0) ? 12 : h % 12;
                var ampm = startHour == 0 ? "AM" : "PM";
                return new HourItem(d.ToString(), ampm, []);
            })
            .ToArray();

    public static string[] Minutes1 => ["15", "30", "45"];

    public static string?[] Minutes2 { get; } = GenerateMinutes2();

    private static string?[] GenerateMinutes2() =>
        [
            .. Enumerable.Range(2, 23)
                .Select(i => (string?)(i * 30).ToString())
,
            "1440",
        ];

    public TimeSpan TotalMinutes => TimeSpan.FromMinutes(MinutesSlider.Value);

    public static readonly DependencyProperty InStartupOptionsProperty = DependencyProperty.Register(nameof(InStartupOptions), typeof(bool), typeof(TimeSliderControl), new PropertyMetadata(default(bool)));

    public TimeSliderControl()
    {
        InitializeComponent();
    }

    public bool InStartupOptions
    {
        get => (bool)GetValue(InStartupOptionsProperty);
        set => SetValue(InStartupOptionsProperty, value);
    }

    private void MenuItemOnClick_ActiveFor(object sender, RoutedEventArgs e)
    {
        var menu = (MenuItem)sender;
        if (menu.Tag == null)
        {
            return;
        }

        MinutesSlider.Value = int.Parse((menu.Tag as string)!);
    }

    private void QuarterButton_Click(object sender, RoutedEventArgs e)
    {
        var time = Routines.ContentToTimeSpan(sender);
        if (time != TimeSpan.MaxValue)
        {
            var dt = Routines.GetDateTimeFromTimeSpan(time, Awakeness.AwakenessTypes.Absolute);
            var ts = Routines.GetRelativeTimeSpanFromDateTime(dt);
            MinutesSlider.Value = (int) ts.TotalMinutes;
        }
        e.Handled = true;
    }

    internal void SetRelativeTime(TimeSpan relativeSpan) => MinutesSlider.Value = (int) relativeSpan.TotalMinutes;

    private void HourButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender == null)
        {
            return;
        }

        var button = (Button)sender;
        var ampm = button.Tag as string ?? string.Empty;
        var hour = int.Parse(button.Content.ToString() ?? "0") + (ampm == "PM" ? 12 : 0);
        var dt = Routines.GetDateTimeFromTimeSpan(TimeSpan.FromHours(hour), Awakeness.AwakenessTypes.Absolute);
        var ts = Routines.GetRelativeTimeSpanFromDateTime(dt);
        MinutesSlider.Value = ts.TotalMinutes;
        e.Handled = true;
    }
}

public record QuarterItem(string Label, string Minutes);
public record HourItem(string HourLabel, string AmPm, IReadOnlyList<QuarterItem> Quarters);

